# Activity 2 — 自建 MCP Server:給 agent 造工具

## 練習 0 — Playwright MCP

`claude mcp add playwright -- npx @playwright/mcp@latest` 一開始連不上:
這台機器的 Node 是 18.18.2,而 `@playwright/mcp@latest`(以及它拉進來的
`playwright-core` 1.6x alpha)在啟動時直接印「Playwright requires Node.js 20
or higher」然後退出,和 npm 的 `engines` 欄位無關,是套件自己在 runtime 判斷。

解法:pin 到 `@playwright/mcp@0.0.29`,它依賴的 `playwright-core@1.53.0` 還沒
加這個檢查。但 0.0.29 綁的 `@modelcontextprotocol/sdk@^1.11.0` 太舊,吐出來的
tool schema 被 Claude Code 現在的 client 判定不合法(`tools/0.inputSchema.type`
驗證失敗)。最後選 `@playwright/mcp@0.0.50`(`playwright-core@1.58.0-alpha`,
還沒踩到 Node 20 檢查;`sdk@^1.17.5`,schema 格式夠新)兩邊都過。

用瀏覽器自動化建立訂單、截圖結果頁(訂單 #208:陳志明/金卡會員,
SKU-1007 極光 降噪耳機 ×3,10% 折扣後應付 NT$5,211.00,截圖存在
`.playwright-mcp/order-208-result.png`),對照活動 1 練習 2 人工重現 bug
的步驟:人工要一步步找表單欄位、填值、送出、再手動截圖確認;有了工具之後
這些步驟還是要做,但不用我自己讀 HTML 猜欄位名稱——agent 讀 accessibility
snapshot 就知道每個欄位的 ref,自己選客戶、選商品、填數量、送出、確認結果、
截圖,一次工具呼叫串完整個流程。差別不是「不用做」,而是「不用我逐步下
每個指令,agent 自己串起完整流程」。

## 練習 1~2 — OrderHub.Mcp 唯讀工具

三個工具(`get_order`/`low_stock`/`customer_orders`)照文件寫完後,`dotnet build`
過,但實際呼叫時全部吃 SQL 登入失敗:

```
無法開啟登入所要求的資料庫 "OrderHubTraining"。登入失敗。
使用者 'JPOSDEV408\dm80' 登入失敗。
```

根因:這台機器的 SQL Server 是具名執行個體 `.\MSSQLSERVERMT`,不是預設執行個體
`localhost`。`OrderHub.Web` 的 `appsettings.Development.json` 本機已經有這條
（未提交的本機覆寫),但 `OrderHub.Mcp` 是全新專案,一開始完全沒有 `appsettings.json`,
只靠 `Program.cs` 裡的硬寫字串 fallback(`Server=localhost;...`)。

補上 `appsettings.json`(沿用 Web 的通用預設值,會進 git)與
`appsettings.Development.json`(本機覆寫,不進 git,和 Web 的模式一致)後,
還是連不到本機那台——因為 Generic Host 預設把 **啟動時的當前工作目錄** 當作
ContentRootPath 去找設定檔,而 MCP client 可能從任何目錄啟動這個 stdio
process,不保證等於專案資料夾。加一行
`ContentRootPath = AppContext.BaseDirectory` 把設定檔搜尋錨定在執行檔自己的
目錄,問題才真正解決。

驗證:因為沒有瀏覽器,改寫一個小型 Node MCP client 直接對 `dotnet run
--project src/OrderHub.Mcp` 說 stdio protocol(等同 Inspector 手動點的操作)。
`low_stock` 回傳的 5 個 SKU 和 `/Products/LowStock` 頁面逐一比對,順序、數量
完全一致;`get_order` 用不存在的 Id 回傳「找不到訂單 999999」,不是 exception
dump。

## 練習 3 — before/after 對照

**Before**(`.mcp.json` 還沒建立、orderhub 工具不存在時),要回答「哪些商品
庫存低於 5?」,我能做的是:

1. 猜路由:`/Products/LowStock`(得先看過 nav bar 才知道有這個頁面)
2. 猜參數名稱:先試 `?threshold=5`(小寫)沒反應,看了表單原始碼才知道要
   `?Threshold=5`(大寫開頭,ASP.NET model binding 大小寫敏感的坑)
3. 沒有結構化資料,只能開瀏覽器導覽、讀 accessibility snapshot,肉眼比對表格
   欄位(現有庫存 vs 近 30 天售出數量,兩欄都是數字,一開始用 `curl | grep`
   爬字串時完全分不出哪個數字對應哪一欄)

**After**(呼叫 `low_stock(threshold=5)` 一次):回傳結構化 JSON
(`Sku`/`Name`/`StockQuantity`),五筆資料、順序、數量和 Before 完全一致,
但不用猜路由、不用管大小寫、不用肉眼比對表格——一次呼叫就是答案。

**中間又踩了一個坑**:`orderhub` 註冊進 `.mcp.json` 後,`claude mcp list`
顯示 `Connected`,但工具怎麼樣都不出現在對話裡,呼叫直接報
`No such tool available`。兩層原因:

1. 新專案層級的 `.mcp.json` server 第一次出現時,信任(approve)提示只在
   **啟動 `claude`** 時跳出來,`/mcp` 的 reconnect 只對已經信任過的 server
   生效——单純在同一個 session 裡按 `/mcp` 永遠等不到 orderhub 的信任提示,
   要整個退出重進(`claude --continue`)才會問。
2. 信任之後又卡了一次:`claude mcp list` 自己起一個獨立行程做健康檢查,跟
   目前對話這個 session 實際持有的 MCP 連線池是兩件事——`list` 顯示
   Connected 不代表這個對話馬上摸得到工具,一樣需要 `/mcp` 讓**這個**
   session 重新拉一次工具清單。
3. 工具終於出現後,呼叫 `low_stock` 又是一片「An error occurred invoking
   'low_stock'」——因為 `.mcp.json` 沒帶任何 env,啟動時 `DOTNET_ENVIRONMENT`
   是空的,Generic Host 預設環境是 Production,只載入通用的
   `appsettings.json`(`Server=localhost`),不會載入本機真正需要的
   `appsettings.Development.json`。在 `.mcp.json` 幫 orderhub 加一段
   `"env": { "DOTNET_ENVIRONMENT": "Development" }` 才解決——這段可以放心
   進 git,「用 Development 環境」是通用設定,机器差異只在
   `appsettings.Development.json` 的實際連線字串裡(那份本來就不進 git)。

## 練習 4 — cancel_order(會改資料的工具)

三個唯讀工具補上 `[McpServerTool(ReadOnly = true)]`,新增
`[McpServerTool(Destructive = true, Idempotent = false)]` 的 `cancel_order`,
直接轉呼叫既有的 `OrderService.CancelOrderAsync`(狀態檢查、庫存回補都在
service 層,工具不重複實作)。

驗證(用同一支 Node MCP client 腳本,`tools/list` 讀 annotations、
`tools/call` 實際跑一輪):

- annotations 正確:三個唯讀工具 `readOnlyHint: true`;`cancel_order`
  `destructiveHint: true, idempotentHint: false`
- 訂單 #208(SKU-1007 ×3)取消成功,訊息「訂單 208 已取消,庫存已回補」,
  `/Products` 上 SKU-1007 庫存確實從 49 回補到 52
- 對同一筆訂單再取消一次:「取消失敗:狀態為 Cancelled 的訂單不可取消」
  ——清楚的業務錯誤,不是 exception dump

**改用真正的 agent 呼叫(不是腳本)重跑一次**,順便驗證權限確認提示這件事:
開一筆新訂單(#209,SKU-1001 ×1),讓 Claude Code 直接呼叫
`mcp__orderhub__cancel_order(id=209)`——**完全沒有跳出允許/拒絕的確認
提示,直接執行成功**。這對照文件裡的地雷區提醒很貼切:annotations
（`destructiveHint` 等)只是給 client 的 hint,不是強制規範;這台機器
目前的權限設定顯然把 MCP tool call 當成已授權的操作,沒有額外把
destructive 標註接到確認流程上。換句話說:**真正的授權檢查不能只依賴
annotation**,要做在 server/service 層——這正是這個練習的重點,而不是
巧合地被印證了一次。
