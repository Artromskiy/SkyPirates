$ErrorActionPreference = "Stop"

$BRANCH = git branch --show-current
$REMOTE = "origin"

Write-Host "Current branch: $BRANCH"

# 1. Проверяем изменения в .gitignore
$gitignoreChanged = git status --porcelain .gitignore

if ($gitignoreChanged) {
    Write-Host "Updating .gitignore..."

    git add .gitignore
    git commit -m "Update .gitignore"
}

# 2. Применяем .gitignore (очистка индекса)
Write-Host "Refreshing index based on .gitignore..."

try {
    git rm -r --cached . | Out-Null
} catch {}

git add .

# коммит может не создаться, если нет изменений — это нормально
try {
    git commit -m "Apply .gitignore cleanup"
} catch {}

# 3. Получаем коммиты, которых нет на remote
$COMMITS = git rev-list --reverse "$REMOTE/$BRANCH..$BRANCH"

foreach ($COMMIT in $COMMITS) {
    Write-Host "Pushing commit $COMMIT"

    git checkout -b temp_push "$REMOTE/$BRANCH"
    git cherry-pick $COMMIT

    git push $REMOTE "temp_push:$BRANCH"

    git checkout $BRANCH
    git branch -D temp_push

    Write-Host "Done $COMMIT"
}

Write-Host "All commits pushed one by one"