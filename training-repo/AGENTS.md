# OrderHub — 專案記憶

## 專案簡介

公司內部訂單管理系統：業務可建立/查詢訂單、管理商品與客戶。
內部使用、單一 SQL Server 資料庫，不需要考慮多租戶或高併發架構。

## 技術棧

- .NET 8 / ASP.NET Core MVC（Razor Views）
- EF Core 8 + SQL Server
- 測試：xUnit

## 分層與慣例

- 三層：`OrderHub.Web`（Controller/View/ViewModel）→ `OrderHub.Core`
  （Domain/Services/Interfaces）→ `OrderHub.Infrastructure`（Repositories/Migrations）
- Controller 保持薄，只轉接 service 結果；商業邏輯一律放 Core 的 service
- 只有 repository 碰 `DbContext`；Controller / Service 不可直接用 EF Core
- Service 回傳 `ServiceResult<T>`，用它表達預期內的失敗，不要丟例外
- View 綁 ViewModel，不要把 domain model 直接丟給 View
- 使用者輸入用 DataAnnotations + ModelState 驗證；輸入錯誤絕不能變成 500
- 金額一律用 `decimal`；折扣集中在 `OrderService.CalculateTotal`，不要在別處重算
- 參考檔：Controller 照 `ProductsController.cs`、Service 照 `ProductService.cs` 的寫法
- `OrderHub.Mcp`（`src/OrderHub.Mcp`）是第四個專案：獨立的 MCP server（console app），
  直接參考 `OrderHub.Core` + `OrderHub.Infrastructure`，不經過 `OrderHub.Web`。
  它有自己的 `appsettings.Development.json`（獨立連線字串，需與 Web 端保持同步）。

## AI 訂單查詢（Gemini 自然語言搜尋）

- 端點：`POST /api/orders/search`（`OrdersApiController`，JSON API）與
  `GET /Orders/Search?q=`（`OrdersController.Search`，畫面版），body/query 都是 `{ text }` 一句話。
- 流程：`OrderSearchService.SearchAsync` → `IOrderQueryTranslator.TranslateAsync`
  （實作在 `GeminiOrderQueryTranslator`）呼叫 Gemini，把自然語言轉成結構化
  `OrderSearchQuery`（status / memberTier / dateFrom / dateTo）→ `IOrderRepository.SearchAsync`。
- 白名單防線（在程式碼裡，不只靠 prompt）：Gemini 回傳的 `intent` 必須是 `"search"`
  且至少要有一個有效條件（`HasAnyFilter`），否則一律回 `422 無法理解的查詢`。
  非查詢意圖（例如要求刪除/修改資料）會被分類成 `unsupported` 並同樣被拒絕——
  且此端點本來就沒有任何刪改能力，就算模型誤判也無法造成破壞。
- **相對日期一定要注入今天的日期**：prompt 樣板（`GeminiOrderQueryTranslator.PromptTemplate`）
  用 `DateTime.Today` 換算「今天是 {0}」；沒有這行，「上個月/上週」這類相對時間會被模型
  瞎猜成不相關的日期。修改 prompt 時務必保留這段。
- 依賴 `Gemini:ApiKey`（來自 `dotnet user-secrets`，`UserSecretsId` 需為
  `dd6219e7-ab3a-4c0b-9581-472abd7910ad`，或環境變數 `GEMINI_API_KEY`）。
  金鑰遺失/`UserSecretsId` 對不上時，端點回 `503`（`AiServiceUnavailableException`），
  其餘頁面不受影響。

## 常用指令

- `dotnet build`：建置
- `dotnet test`：跑全部測試
- `dotnet run --project src/OrderHub.Web`：啟動網站（http://localhost:5150）

## 重要 / 危險檔案

- `src/OrderHub.Infrastructure/Migrations/**`：EF migration 是歷史紀錄，不要手改
- `src/OrderHub.Web/appsettings.json`：連線字串等設定，改動前先問
- `src/OrderHub.Web/OrderHub.Web.csproj` 的 `UserSecretsId`：改動或清空會讓
  `Gemini:ApiKey` 讀不到（AI 搜尋變 503），且不會出現在 build 錯誤裡，只會在執行期才發現——
  改動前先確認 `%APPDATA%\Microsoft\UserSecrets\` 底下對應的資料夾是否存在。

## Hooks

- 對可能破壞資料的 SQL 先套用 `.codex/hooks/block-destructive-sql.ps1`。它會拒絕包含 `DROP TABLE` 或 `TRUNCATE` 的命令；不得繞過此保護。
- `.codex/hooks/log-edits.ps1` 會將檔案編輯記錄到 `.codex/hooks/edit-log.txt`。保留這份稽核紀錄，除非任務明確要求，否則不要修改或刪除它。

## 操作規則

- 永遠禁止 `rm -rf`、`git push --force` 與 `git reset --hard`。
- 執行 `dotnet ef database drop` 或 `git push` 前，必須先取得使用者明確同意。
- `dotnet build`、`dotnet test`、`dotnet run`、`git status`、`git diff`、`git log`、`git add` 與 `git commit` 可作為例行本機操作執行。

## 專用代理

- `code-reviewer`：僅讀取變更，依嚴重度檢查分層、ViewModel/驗證、`decimal` 與 `OrderService.CalculateTotal` 的金額規則，以及回歸測試。每項發現應附檔案、行號與具體建議；沒有發現時也要明確說明。
- `test-runner`：只執行 `dotnet test`，不得修改程式碼。測試通過時回報通過、失敗與略過數；失敗時列出每個失敗測試、斷言訊息及可能原因。

## 不要做的事

- 不要未經同意就加新的 NuGet 套件
- 不要在 Controller / Service 直接使用 DbContext
- 不要為了「順手」重構與當前任務無關的程式碼
- 不要讀取或寫入任何機密檔（appsettings.Production.json、user-secrets）
