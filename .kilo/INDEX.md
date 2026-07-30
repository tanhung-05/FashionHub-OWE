# Kilo Configuration Index

Quick navigation for all configuration files.

## 🚀 Start Here

| File | Purpose | When to Use |
|------|---------|-------------|
| [AGENTS.md](../AGENTS.md) | **Main entry point** | Read this FIRST |
| [QUICKSTART.md](QUICKSTART.md) | Quick start guide | When you're new to the project |
| [SUMMARY.md](SUMMARY.md) | Complete overview | For comprehensive understanding |

## 📚 Rules & Guidelines

| File | Purpose |
|------|---------|
| [project-context.md](project-context.md) | Project overview, status, architecture decisions |
| [architecture.md](architecture.md) | Technical architecture, layers, patterns |
| [coding-standards.md](coding-standards.md) | C# & ASP.NET Core coding standards |
| [git-workflow.md](git-workflow.md) | Git conventions, commit messages, PR workflow |
| [testing-guidelines.md](testing-guidelines.md) | xUnit patterns, integration tests, best practices |

## ⚙️ Commands

| Command | Purpose | Example |
|---------|---------|---------|
| [build.md](command/build.md) | Build & analyze warnings | `/build` |
| [test.md](command/test.md) | Run tests with detailed output | `/test` or `/test cart` |
| [verify.md](command/verify.md) | Full verification pipeline | `/verify` |
| [security.md](command/security.md) | Security scan (secrets + packages) | `/security` |
| [deploy.md](command/deploy.md) | Deployment readiness check | `/deploy check` |

## 🎯 Skills (Workflows)

| Skill | Purpose | When to Use |
|-------|---------|-------------|
| [test-fixing.md](skill/test-fixing.md) | Systematic test fixing workflow | When tests are failing |
| [database-migration.md](skill/database-migration.md) | Database-first EF Core workflow | When changing DB schema |

## 📂 Directory Structure

```
.kilo/
├── INDEX.md                      ← You are here
├── README.md                     ← Configuration documentation
├── SUMMARY.md                    ← Complete overview
├── QUICKSTART.md                 ← Quick start guide
├── project-context.md            ← Project overview
├── architecture.md               ← Architecture details
├── coding-standards.md           ← Code standards
├── git-workflow.md               ← Git conventions
├── testing-guidelines.md         ← Test patterns
├── .gitignore                    ← Ignore node_modules
│
├── command/                      ← Executable commands
│   ├── build.md
│   ├── test.md
│   ├── verify.md
│   ├── security.md
│   └── deploy.md
│
├── skill/                        ← Domain workflows
│   ├── test-fixing.md
│   └── database-migration.md
│
└── agent/                        ← Agent configs (future)
```

## 🔍 Quick Reference

### Common Tasks

| Task | Files to Read |
|------|---------------|
| **Fix failing tests** | [skill/test-fixing.md](skill/test-fixing.md) |
| **Add new feature** | [coding-standards.md](coding-standards.md) + [architecture.md](architecture.md) |
| **Change database** | [skill/database-migration.md](skill/database-migration.md) |
| **Commit code** | [git-workflow.md](git-workflow.md) |
| **Deploy** | [command/deploy.md](command/deploy.md) + `docs/docker-deployment.md` |
| **Security review** | [command/security.md](command/security.md) |

### By Role

**New Agent:**
1. [AGENTS.md](../AGENTS.md)
2. [QUICKSTART.md](QUICKSTART.md)
3. [project-context.md](project-context.md)

**Developer (Coding):**
1. [coding-standards.md](coding-standards.md)
2. [architecture.md](architecture.md)
3. [testing-guidelines.md](testing-guidelines.md)

**DevOps (Deployment):**
1. [command/deploy.md](command/deploy.md)
2. [command/security.md](command/security.md)
3. `docs/docker-deployment.md`

**QA (Testing):**
1. [testing-guidelines.md](testing-guidelines.md)
2. [skill/test-fixing.md](skill/test-fixing.md)
3. [command/test.md](command/test.md)

## 📊 Project Status

**Last Verified:** 2026-07-29

- **Build:** ✅ SUCCESS (24 warnings)
- **Tests:** ⚠️ 29/32 PASS (3 failing - root causes known)
- **Controllers:** ✅ 13 total
- **Git:** ✅ 37 commits, v1.0.0
- **Docker:** ✅ Ready

See [project-context.md](project-context.md) for detailed status.

## 🎯 Priority Tasks

1. Fix 3 failing tests → [skill/test-fixing.md](skill/test-fixing.md)
2. Verify API key rotation → [command/security.md](command/security.md)
3. Run full verification → [command/verify.md](command/verify.md)

## 📖 External Documentation

- `docs/production-readiness-report.md` - Production readiness
- `docs/docker-deployment.md` - Docker deployment guide
- `docs/gemini-api-key-setup.md` - API key configuration

## 💡 Tips

- **Always start with evidence** - Run commands, include output
- **Follow immutable decisions** - Don't change architecture
- **Use PowerShell 5.1 syntax** - Use `;` not `&&`
- **Read before acting** - Check relevant files first
- **Test incrementally** - Build + test after each change

---

**Configuration Version:** 1.0.0  
**Last Updated:** 2026-07-29  
**Format:** Kilo Configuration Standard
