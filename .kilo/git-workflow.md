# Git Workflow & Best Practices

## Branch Strategy

### Main Branches
- **main**: Production-ready code, protected branch
- **develop**: Integration branch for features (if needed for team work)

### Feature Branches
```bash
# Create feature branch
git checkout -b feature/add-wishlist-functionality
git checkout -b fix/cart-quantity-validation
git checkout -b refactor/extract-pricing-service
```

### Branch Naming Convention
- `feature/{description}` - New features
- `fix/{description}` - Bug fixes
- `refactor/{description}` - Code refactoring
- `test/{description}` - Test additions/updates
- `docs/{description}` - Documentation updates
- `chore/{description}` - Maintenance tasks

## Commit Guidelines

### Commit Message Format
```
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

### Types
- **feat**: New feature
- **fix**: Bug fix
- **refactor**: Code refactoring (no functional change)
- **test**: Add or update tests
- **docs**: Documentation only changes
- **style**: Code style (formatting, missing semicolons, etc.)
- **perf**: Performance improvements
- **chore**: Maintenance tasks (dependencies, build scripts)

### Examples
```bash
feat(cart): add coupon code validation

Implement server-side coupon validation:
- Check date range validity
- Verify minimum order value
- Validate usage limit per user

Closes #42

---

fix(products): resolve null reference in Details action

Add null check before accessing IddanhMucNavigation
in ProductsController.Details() to prevent crash
when product has no category assigned.

Fixes #87

---

refactor(services): extract pricing logic to PricingService

Move discount and tax calculation from OrderController
to dedicated PricingService for better testability.

No functional changes.
```

## Before Committing

### 1. Check Status
```powershell
git status
```

### 2. Review Changes
```powershell
# Review all changes
git diff

# Review staged changes
git diff --staged

# Review specific file
git diff FashionHub2/FashionHub.Web/Controllers/CartController.cs
```

### 3. Stage Files Selectively
```powershell
# Stage specific files only
git add FashionHub2/FashionHub.Web/Controllers/CartController.cs
git add FashionHub2/FashionHub.Web/Views/Cart/Index.cshtml

# NEVER use git add . blindly - you might commit secrets!
```

### 4. Security Check
```powershell
# Check for hardcoded secrets before committing
Select-String -Path "FashionHub2\**\*.cs" -Pattern "AIzaSy" -SimpleMatch
Select-String -Path "FashionHub2\**\*.json" -Pattern "password" -SimpleMatch

# Ensure appsettings.json has no real secrets
cat FashionHub2/FashionHub.Web/appsettings.json
```

### 5. Build & Test
```powershell
cd FashionHub2
dotnet build
dotnet test
```

### 6. Commit
```powershell
git commit -m "feat(cart): add coupon validation"
```

## Common Workflows

### Feature Development
```powershell
# 1. Create feature branch
git checkout -b feature/add-wishlist

# 2. Make changes, commit incrementally
git add FashionHub2/FashionHub.Web/Controllers/WishlistController.cs
git commit -m "feat(wishlist): add WishlistController"

git add FashionHub2/FashionHub.Web/Views/Wishlist/
git commit -m "feat(wishlist): add wishlist views"

# 3. Push to remote
git push -u origin feature/add-wishlist

# 4. Create PR (using GitHub CLI)
gh pr create --title "Add wishlist functionality" --body "Implements user wishlist feature with add/remove/view capabilities"

# 5. After PR approved and merged, cleanup
git checkout main
git pull
git branch -d feature/add-wishlist
```

### Bug Fix
```powershell
# 1. Create fix branch
git checkout -b fix/cart-null-reference

# 2. Fix the bug
# Edit files...

# 3. Test the fix
cd FashionHub2
dotnet test

# 4. Commit with reference to issue
git add FashionHub2/FashionHub.Web/Controllers/CartController.cs
git commit -m "fix(cart): handle null variant in AddToCart

Add null check before accessing variant properties.
Fixes #123"

# 5. Push and create PR
git push -u origin fix/cart-null-reference
gh pr create --title "Fix: Cart null reference when variant not found" --body "Fixes #123"
```

### Hotfix (Critical Production Bug)
```powershell
# 1. Branch from main
git checkout main
git pull
git checkout -b hotfix/security-vulnerability

# 2. Make minimal changes to fix critical issue
# Edit files...

# 3. Test thoroughly
cd FashionHub2
dotnet build
dotnet test

# 4. Commit and push immediately
git commit -am "fix(security): patch SQL injection vulnerability"
git push -u origin hotfix/security-vulnerability

# 5. Create PR with high priority
gh pr create --title "[HOTFIX] Security vulnerability patch" --body "Critical security fix" --label "security,priority:high"
```

## What NOT to Commit

### Files to NEVER Commit
- `appsettings.Development.json` with real connection strings
- `appsettings.Production.json` with real secrets
- `.env` files with real API keys
- `*.user` files (VS/Rider user settings)
- `bin/` and `obj/` folders (already in .gitignore)
- Database backup files (`*.bak`)
- Large binary files
- Personal notes or TODO lists

### Check .gitignore
```gitignore
# Already should be in .gitignore
bin/
obj/
*.user
.vs/
.vscode/
*.suo
*.cache

# Add if not present
appsettings.Development.json
appsettings.Production.json
.env
*.bak
*.log
```

## Undo Operations

### Undo Uncommitted Changes
```powershell
# Discard changes in working directory
git restore FashionHub2/FashionHub.Web/Controllers/CartController.cs

# Unstage file (keep changes in working directory)
git restore --staged FashionHub2/FashionHub.Web/Controllers/CartController.cs

# Discard all uncommitted changes (DANGEROUS!)
git restore .
```

### Undo Last Commit (Not Pushed)
```powershell
# Keep changes in working directory
git reset --soft HEAD~1

# Discard changes completely (DANGEROUS!)
git reset --hard HEAD~1
```

### Fix Last Commit Message
```powershell
git commit --amend -m "fix(cart): correct commit message"
```

### Revert Pushed Commit
```powershell
# Create new commit that undoes changes
git revert abc123

# Push the revert
git push
```

## Viewing History

### Log Commands
```powershell
# Compact one-line log
git log --oneline -20

# Graph view
git log --oneline --graph --all -20

# See what changed in each commit
git log -p -5

# Search commits
git log --grep="cart"
git log --author="username"

# File history
git log --oneline -- FashionHub2/FashionHub.Web/Controllers/CartController.cs
```

### Show Specific Commit
```powershell
# Show commit details
git show abc123

# Show files changed
git show --stat abc123

# Show specific file in commit
git show abc123:FashionHub2/FashionHub.Web/Controllers/CartController.cs
```

## Tags & Releases

### Create Tag
```powershell
# Lightweight tag
git tag v1.0.0

# Annotated tag (recommended)
git tag -a v1.0.0 -m "Release version 1.0.0 - Production ready"

# Push tag to remote
git push origin v1.0.0

# Push all tags
git push --tags
```

### List Tags
```powershell
git tag
git tag -l "v1.*"
```

### Delete Tag
```powershell
# Delete local tag
git tag -d v1.0.0

# Delete remote tag
git push origin :refs/tags/v1.0.0
```

## Pull Request Best Practices

### PR Title
```
feat(cart): Add coupon code functionality
fix(products): Resolve image loading issue in product details
refactor(services): Extract pricing logic to service layer
```

### PR Description Template
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Refactoring
- [ ] Documentation update

## Changes Made
- Added CouponController with CRUD operations
- Updated CartController to apply coupons
- Added coupon validation logic
- Created coupon management views in Admin area

## Testing
- [x] All existing tests pass
- [x] Added new tests for coupon functionality
- [x] Manual testing completed

## Screenshots (if applicable)
[Add screenshots of UI changes]

## Related Issues
Closes #42
Related to #38

## Checklist
- [x] Code follows project coding standards
- [x] Self-reviewed the code
- [x] Commented complex logic
- [x] Updated documentation
- [x] No secrets/credentials committed
- [x] Build succeeds with no errors
- [x] Tests pass
```

### Before Creating PR
```powershell
# 1. Ensure branch is up to date
git checkout main
git pull
git checkout feature/add-coupon
git merge main

# 2. Run full test suite
cd FashionHub2
dotnet build
dotnet test

# 3. Check for any debug code or console logs
Select-String -Path "FashionHub2\**\*.cs" -Pattern "Console.WriteLine" -SimpleMatch
Select-String -Path "FashionHub2\**\*.cs" -Pattern "TODO" -SimpleMatch

# 4. Push to remote
git push -u origin feature/add-coupon

# 5. Create PR
gh pr create --title "Add coupon code functionality" --body "$(cat .github/pull_request_template.md)"
```

## Merge Strategies

### Squash and Merge (Recommended for features)
- Combines all commits into one
- Keeps main branch history clean
- Use for: Feature branches with many WIP commits

### Merge Commit (For important milestones)
- Preserves all commits
- Creates a merge commit
- Use for: Release branches, important milestones

### Rebase and Merge (For clean linear history)
- Replays commits on top of base branch
- No merge commit
- Use for: Small, clean branches with few commits

## Collaboration Guidelines

### Before Starting Work
```powershell
# Always pull latest changes
git checkout main
git pull origin main
```

### Syncing Feature Branch
```powershell
# Keep feature branch updated with main
git checkout feature/add-wishlist
git merge main

# Or rebase (rewrites history - use carefully)
git rebase main
```

### Resolving Conflicts
```powershell
# 1. Conflicts occur during merge
git merge main
# CONFLICT in FashionHub2/FashionHub.Web/Controllers/CartController.cs

# 2. Open conflicted files, resolve conflicts manually
# Look for <<<<<<< HEAD, =======, >>>>>>> main markers

# 3. Stage resolved files
git add FashionHub2/FashionHub.Web/Controllers/CartController.cs

# 4. Complete merge
git commit -m "Merge main into feature/add-wishlist"

# 5. Test after merge!
cd FashionHub2
dotnet build
dotnet test
```

## Emergency Procedures

### Accidentally Committed Secret
```powershell
# 1. DO NOT just remove it in next commit - it's still in history!

# 2. Remove from last commit (if not pushed)
git reset --soft HEAD~1
# Remove the secret from files
git add .
git commit -m "feat(cart): add feature (without secret)"

# 3. If already pushed - CRITICAL
# - Immediately rotate the compromised secret (API key, password, etc.)
# - Use git filter-branch or BFG Repo-Cleaner to remove from history
# - Force push (coordinate with team!)
# - Document the incident

# 4. Prevent future incidents
# Add to .gitignore
# Set up pre-commit hooks to scan for secrets
```

### Pushed to Wrong Branch
```powershell
# 1. Create correct branch from current state
git checkout -b correct-branch

# 2. Push correct branch
git push -u origin correct-branch

# 3. Reset wrong branch (if you have access)
git checkout wrong-branch
git reset --hard origin/wrong-branch

# 4. Force push reset (COORDINATE WITH TEAM!)
git push --force
```

## Git Configuration

### User Settings
```powershell
# Set name and email
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"

# Set default editor
git config --global core.editor "code --wait"

# Set line endings (Windows)
git config --global core.autocrlf true
```

### Useful Aliases
```powershell
git config --global alias.st status
git config --global alias.co checkout
git config --global alias.br branch
git config --global alias.ci commit
git config --global alias.unstage 'restore --staged'
git config --global alias.last 'log -1 HEAD'
git config --global alias.visual 'log --oneline --graph --all'
```

## Integration with GitHub CLI

### Setup
```powershell
# Install GitHub CLI
winget install GitHub.cli

# Authenticate
gh auth login
```

### Common Commands
```powershell
# Create PR
gh pr create --title "Add feature" --body "Description"

# List PRs
gh pr list

# View PR
gh pr view 123

# Check PR status
gh pr checks

# Merge PR
gh pr merge 123 --squash

# Create issue
gh issue create --title "Bug: Cart calculation error" --body "Description"

# View issues
gh issue list
```
