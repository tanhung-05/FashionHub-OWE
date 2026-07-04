using Microsoft.ML;
using Microsoft.ML.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace FashionHub.Web.Services;

public class ImageFeatureService : IImageFeatureService
{
    private readonly MLContext mlContext;
    private readonly ITransformer model;

    // Cấu hình ResNet18
    private const int ImageHeight = 224;
    private const int ImageWidth = 224;
    private const string ModelInput = "data";
    private const string ModelOutput = "resnetv22_dense0_fwd";

    public ImageFeatureService(string modelPath)
    {
        mlContext = new MLContext();

        // --- PIPELINE MỚI: CỰC KỲ ĐƠN GIẢN ---
        // Không còn LoadImages, ResizeImages, ExtractPixels nữa.
        // Chúng ta đưa thẳng mảng số (float[]) vào.

        var pipeline = mlContext.Transforms.ApplyOnnxModel(
            modelFile: modelPath,
            outputColumnNames: new[] { ModelOutput },
            inputColumnNames: new[] { ModelInput }
        );

        // Tạo model
        var emptyData = mlContext.Data.LoadFromEnumerable(new List<ModelInputData>());
        model = pipeline.Fit(emptyData);
    }

    public float[] GetFeatureVector(string imagePath)
    {
        // 1. Tự tay xử lý ảnh thành mảng số (float[])
        float[] pixelData = PreprocessImage(imagePath);

        // 2. Tạo dữ liệu đầu vào
        var data = new ModelInputData { data = pixelData };
        var dataView = mlContext.Data.LoadFromEnumerable(new List<ModelInputData> { data });

        // 3. Chạy AI
        var transformedData = model.Transform(dataView);
        var vector = transformedData.GetColumn<float[]>(ModelOutput).FirstOrDefault();

        if (vector == null) throw new Exception("AI trả về kết quả rỗng.");
        return vector;
    }

    // --- HÀM TỰ XỬ LÝ ẢNH (THAY THẾ SKIASHARP) ---
    private float[] PreprocessImage(string imagePath)
    {
        using (var originalImage = new Bitmap(imagePath))
        {
            // A. Resize ảnh về 224x224
            using (var resizedImage = new Bitmap(ImageWidth, ImageHeight))
            {
                using (var g = Graphics.FromImage(resizedImage))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;

                    // Vẽ ảnh lên nền trắng (xử lý PNG trong suốt)
                    g.Clear(Color.White);
                    g.DrawImage(originalImage, 0, 0, ImageWidth, ImageHeight);
                }

                // B. Chuyển đổi Pixel thành mảng float[] theo chuẩn CHW (Channel-Height-Width)
                // Đây là định dạng mà ONNX ResNet yêu cầu
                float[] result = new float[3 * ImageHeight * ImageWidth];

                // --- SỬA LẠI ĐOẠN NÀY TRONG HÀM PreprocessImage ---

                // Các hằng số chuẩn hóa của ImageNet (Bắt buộc cho ResNet)
                float[] mean = new float[] { 0.485f, 0.456f, 0.406f };
                float[] std = new float[] { 0.229f, 0.224f, 0.225f };

                for (int y = 0; y < ImageHeight; y++)
                {
                    for (int x = 0; x < ImageWidth; x++)
                    {
                        Color color = resizedImage.GetPixel(x, y);

                        // Chuẩn hóa: (Giá trị pixel / 255 - Mean) / Std
                        // Sắp xếp theo thứ tự CHW (Channel - Height - Width)

                        // Kênh Red (R)
                        result[0 * ImageHeight * ImageWidth + y * ImageWidth + x] = ((color.R / 255f) - mean[0]) / std[0];

                        // Kênh Green (G)
                        result[1 * ImageHeight * ImageWidth + y * ImageWidth + x] = ((color.G / 255f) - mean[1]) / std[1];

                        // Kênh Blue (B)
                        result[2 * ImageHeight * ImageWidth + y * ImageWidth + x] = ((color.B / 255f) - mean[2]) / std[2];
                    }
                }
                return result;
            }
        }
    }

    // Class dữ liệu đầu vào (Khớp với tên Input của ONNX)
    private class ModelInputData
    {
        [VectorType(3 * ImageHeight * ImageWidth)] // Khai báo kích thước vector
        public float[] data { get; set; } = Array.Empty<float>(); // Tên biến phải trùng với ModelInput ("data")
    }
}