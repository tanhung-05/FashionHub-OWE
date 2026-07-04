namespace FashionHub.Web.Services;

public interface IImageFeatureService
{
    float[] GetFeatureVector(string imagePath);
}