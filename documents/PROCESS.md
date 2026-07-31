# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：

Claude Code（模型 Opus 4.8）

---

## 通用四問

### 1. 我的任務拆解

（開工前你把任務拆成哪幾步？實際做的時候順序有變嗎？為什麼變？）

 - 拆成6步，實際做的時候會有變，因為不是powershell自行關掉就是agent有時需要多次提醒需要注意的一些事項

### 2. AI 幫上大忙的地方

（哪件事 agent 做得又快又好？**貼上當時的提問原文**，說明為什麼這樣問有效。）

 - 很快分析整個project需求，幫你分析好重點（找不到提問原文了因為有開新的session）

### 3. AI 誤導我的地方，與我如何發現

（agent 說錯／改錯／過度自信的時刻。你靠什麼抓到——對照程式碼？頁面實測？跑測試？）

 - 都會叫agent 先寫出他計劃要做的事/需要改動的部分然後檢查是否是我要的需求

### 4. 我會帶回日常工作的一招

（一個具體、可複製的做法，不要寫「要多驗證」這種口號——寫出**操作步驟**。）

 - 1. prompt 明寫「先不要寫程式，給計畫」，並點名要先讀的參考檔（讓它對齊慣例）。
   2. 逐條審計畫：邏輯有沒有跑到該在的那一層、邊界（`<` vs `<=`、過濾條件、日期邊界、排除條件）有沒有覆蓋、有沒有夾帶「順便重構」。
   3. 確認後才放行；實作完**逐條**對規格在頁面/測試驗。
 - **驗證頁面前先確認跑的是新 build**：改完 → 停掉舊程序（`taskkill //PID <pid> //F`）→ `dotnet build` → 重新 `dotnet run` → 再開頁面。不要對著一個「開著沒關」的 server 驗，很可能是舊的。

## 自我驗證（做到哪個階段答哪題）

### 第一階段 — Agentic Coding

練習 1

1. 我能不看筆記說出三個專案（Web/Core/Infrastructure）各自的職責
 - Web＝Controller/View/ViewModel 只接線與顯示
 - Core＝Domain 與 service 商業邏輯（折扣、庫存、狀態轉移）
 - Infrastructure＝EF Core DbContext / repository / migration / 種子資料。
2. 我核對過 agent 描述的建單流程，且**至少找出一處不精確或過度簡化的說法**
3. 我知道商業邏輯應該放在哪一層、新增頁面要動哪些地方

練習 2

1. 三個 bug 我都先在頁面上重現過，才開始找程式
 - 對
2. 我給 agent 的資訊包含具體觀察（頁碼／金額數字／庫存數字），而不是只貼客訴原文
 - 沒有我都是給agent 查看md file 它自動檢查
3. 每個修復都回到頁面驗證過症狀消失
 - 都會先確保before and after 是否修復的都正確
4. 每個 bug 都補了一個回歸測試，`dotnet test` 全綠
 - 都有補回歸測試
5. 三個獨立 commit，message 說明症狀與根因
 - 都有提供
6. （思考題）為什麼原本的測試沒抓到這三個 bug？
 - 因為一開始專注的部分不是在於bug 而是測試流程

練習 3

1. `/Products/LowStock` 不帶參數 → 門檻 10 的結果；帶 `?threshold=3` → 結果隨之改變
 - 不帶參數＝門檻 10（顯示 5 筆）；`?threshold=3` → 1 筆（只剩庫存 2 那筆），結果隨門檻改變。
2. `?threshold=0`、`?threshold=-1` → 頁面顯示驗證錯誤，不是 500
 - `?threshold=0`、`?threshold=-1` → HTTP **200** 並顯示驗證錯誤「Please enter a value greater than or equal to 1」，不是 500。
3. 售出數量欄位排除了 Cancelled 訂單（可用一筆已取消的訂單驗證）
 -  近 30 天售出量排除 Cancelled：測試以「有效 4 + 有效 3 + 已取消 100 + 40 天前 50」驗證結果為 **7**。
4. 停售（已停售 badge）商品不出現在列表
 - 停售商品不出現：測試 `SKU-OFF`（IsActive=false）被排除。
5. 程式分層與命名跟既有的 Products 功能一致（請 agent 自我 review 一次，並自己確認）
 - 分層/命名一致：`code-reviewer` agent 審過確認符合慣例（Controller 薄、邏輯在 Core、只有 repository 碰 DbContext、`ServiceResult`、ViewModel + DataAnnotations），我也逐檔對照確認。
6. 至少 3 個新測試，`dotnet test` 全綠
 - 4 個新 service 測試（門檻過濾+升冪、排除停售、近 30 天排除 Cancelled、threshold ≤ 0 失敗），`dotnet test` 35 全綠。

練習 4

1. 重構後 `dotnet test` 全綠
 - 重構後 `dotnet test` 35 全綠（含練習 2、3 補的測試）。
2. 我能說出這次重構「改善了什麼、沒有改變什麼」
 - 改善＝把 `CreateOrderAsync` 的表頭驗證、單項驗證、扣庫存副作用三種職責拆開，抽出 `ValidateNewOrder`、`ValidateOrderLine` 兩個純驗證方法；沒改變＝所有錯誤訊息、檢查順序、逐項累積錯誤行為、扣庫存與存
檔時機、`CancelOrderAsync` 與折扣計算全不動。
3. 我有在 code review 的角度看過 diff（不是 agent 說好就好）
 - 逐行看過 diff：確認是純提取，唯一新語法是 `customer!` / `product!`（null 檢查移進 helper 後該處已保證非 null），無邏輯改動。

### 第二階段 — 自建 MCP Server

練習 0（先當使用者：接 Playwright MCP）

1. agent 能自己開瀏覽器完成操作並回傳截圖
 - 可以。我只丟一句「網站在 http://localhost:5150，幫我用瀏覽器建立一筆新訂單，完成後截圖結果頁給我看」，agent 自己走完全程：`/Orders` → 點「建立訂單」→ 客戶選「陳志明（金卡會員）」→ 商品選 SKU-1002 極光 機械鍵盤 → 數量改 2 → 送出 → 停在 `/Orders/Details/206` 截圖。我沒有點過任何一下。

2. 對比活動 1 練習 2 的人工重現步驟
 - **當時**：三張客訴的「重現」都是我自己在頁面上跑。客訴 1 要建一筆訂單記下編號、回第一頁翻找、再點最後一頁看是不是空白；客訴 2 要先去 `/Products` 抄下原價、用金卡客戶下一單、在明細頁**手算** 原價 × 0.9 再跟頁面數字對；客訴 3 要抄庫存 → 下單 → 取消 → 回商品頁看有沒有加回來。每個 bug 光重現就是好幾分鐘的點頁面 + 抄數字，抄完才輪到 agent 從「定位根因」開始幫忙。
 - **現在**：這一整段 agent 自己做。上面那筆 #206 其實就是**客訴 2 的重現配方**——Gold 客戶 + 單價 NT$ 2,320 × 2。結果頁：小計 NT$ 4,640 → 會員折扣（10%）-NT$ 464 → 應付 NT$ 4,176，正好等於手算的 4,640 × 0.9。等於「重現 + 抄數字 + 核對」三步一次跑完，我只看最後那張截圖。
 - **差在哪**：以前流程是「我操作 → 我觀察 → 我把數字餵給 agent」，agent 只能從第 ③ 步接手；接上 Playwright MCP 之後它從第 ① 步就能自己來，我的角色從「操作員」變成「看結果的人」。
 - **沒有變的事**（別誤以為全自動）：網站還是要先自己跑起來（這次是我事先開好的），活動 1 學到的「驗證前先確認跑的是新 build」照樣要自己顧。另外 agent 實際判讀的是頁面的 accessibility snapshot（元素樹），**不是那張圖**——截圖是給人看的證據，不是它的判斷來源，所以純視覺的問題（跑版、顏色、被遮住）它未必看得出來。

練習 3（註冊給 agent，before/after 對照）

1. `/mcp` 能看到 orderhub server 與三個工具
 - 可以。要點：`.mcp.json` 只在 **Claude Code 從 `training-repo` 啟動**時才載入。before 那個 session 的工作目錄是上層的 `D:\AITraining\115-training`，所以拿不到 orderhub 工具；`cd training-repo` 重開一個 session、批准專案 MCP server 之後才看得到 `get_order` / `low_stock` / `customer_orders`。

2. 對照實驗：同一個問題「**哪些商品庫存低於 5？**」

 - **Before（沒有 orderhub 工具，在上層目錄的 session）**——三步 + 一個沒人叫我做的判斷：

   | # | 動作 | 為什麼需要 |
   |---|---|---|
   | 1 | 讀 `src/OrderHub.Core/Domain/Product.cs` | 不知道欄位叫什麼（`StockQuantity`? `Stock`?） |
   | 2 | grep Infrastructure 找 `ToTable` / `DbSet<Product>` | 確認實體對到的表名是 `Products` |
   | 3 | 手寫 ADO.NET + 原始 SQL（`SELECT Sku, Name, StockQuantity FROM Products WHERE StockQuantity < 5 AND IsActive = 1 ORDER BY StockQuantity`）連 DB 查 | 沒有現成介面可問 |

   結果 5 筆：SKU-1048(2)、SKU-1005(3)、SKU-1023(3)、SKU-1032(4)、SKU-1014(4)。
   中間還被我攔下來一次——那句 PowerShell 直連 DB 我按了拒絕（因為我在找 `.mcp.json` 為什麼還沒出現），確認過流程才讓它重跑。**「要人核准」本身也是 before 的成本之一**。

 - **After（同一個問題，`cd training-repo` 重開 session）**——一次工具呼叫：
   `low_stock(threshold=5)` → 直接回 5 筆 JSON，已排序、已排除停售。我輸出的就是那張表，中間沒有讀任何一個檔、沒有碰 DB。
   （嚴格說是「載入工具 schema + 呼叫」兩步：這個 session 的 MCP 工具是 deferred 的，我先 `ToolSearch` 取回 `low_stock` 的參數說明才呼叫。但這是 client 的載入機制，不是我在繞路找答案。）

3. 兩次真正的差異（不是「快一點」而已）

 - **知識從我身上移到 server 裡**。before 的三步全是在補「這個系統長怎樣」的知識：欄位名、表名、連線字串。after 這些都在 `OrderHubTools.LowStock` 裡寫死了，我只需要知道「有一個工具叫 low_stock，參數是門檻」。
 - **連線字串是我「碰巧知道」的**。before 那個 session 前面除錯時剛好發現 DB 在 `localhost\MSSQLSERVER2022`。一個全新的 agent 還要多一步去翻 `appsettings.Development.json`——而且很可能先讀了 `appsettings.json` 裡的 `localhost` 然後連錯、以為 DB 掛了。
 - **`IsActive = 1` 是我自己補的，沒人告訴我**。我是從 `Product.IsActive` 這個欄位推出「應該要排除停售商品」。這一步最危險的不是慢：**漏掉的話結果會多幾筆不該出現的商品，畫面完全正常、不會報錯**。after 版本這條規則長在工具裡（`productRepository.GetActiveAsync()`），我漏不掉。繞遠路的成本是「安靜地答錯」，不是「多花兩分鐘」。
 - **順序的小差異**：兩次都是同一組 5 筆，但庫存都是 4 的那兩筆順序相反（SQL 給 1032→1014，工具給 1014→1032）。同分沒有次要排序鍵，兩邊都對；如果哪天有人拿這個排名做決策，要在工具裡補一個 tie-break。

4. 我會帶走的一句話
 - 寫 MCP 工具真正在做的事，是**把「查這件事要知道的所有前提」封裝起來**——表名、連線、過濾條件。before 的三步不是「agent 比較笨」，是那三個前提沒人給它。

練習 4（會改資料的工具：cancel_order）

1. annotations 如標註，三個唯讀工具顯示 read-only
 - 沒開 Inspector，改用 stdio 直接跟 server 對話（送 `initialize` + `tools/list`）撈出來的原始 JSON：

   ```
   customer_orders: {"readOnlyHint":true}
   get_order:       {"readOnlyHint":true}
   low_stock:       {"readOnlyHint":true}
   cancel_order:    {"destructiveHint":true,"idempotentHint":false}
   ```

   `cancel_order` 沒有 `readOnlyHint` 是對的——依 spec 預設就是 `false`。Inspector 那一頁看的就是這份東西，寫個 30 行 script 也拿得到，不一定要開瀏覽器。

2. 權限確認提示：按允許之前資料不會被動到
 - `.claude/settings.json` 的 `allow` 清單裡**只有 Bash 指令、沒有任何 MCP 工具**，所以工具呼叫走的是預設的逐次確認——`cancel_order` 每一次都要人按過才送得出去。
 - 這裡有個一開始想不到的重點：**annotations 是給 client 看的提示，不是 server 的防線**。`destructiveHint` 只影響 Claude Code 要不要跳確認；真正擋住不該取消的訂單的，是 `OrderService.CancelOrderAsync` 裡的狀態檢查。換一個不理會 annotations 的 client，工具照樣叫得動——所以授權檢查不能外包給對面。

3. 取消一筆待處理訂單，回 `/Products` 確認庫存回補
 - 用 #206（練習 0 用 Playwright 建的那筆，拿它收尾剛好）。前後對照：

   | | 訂單 #206 狀態 | `/Products` 頁面 SKU-1002 庫存 |
   |---|---|---|
   | 取消前 | Pending | **100** |
   | 取消後 | Cancelled | **102** |

   +2 正好等於訂單數量（極光 機械鍵盤 × 2）。這就是活動 1 客訴 3 修好的那個行為，繞過網頁、從 MCP 工具進來也一樣成立——因為規則長在 service 層，不是長在 Controller。

4. 重複取消／已出貨訂單：清楚的拒絕訊息，不是 exception dump
 - `cancel_order(206)` 第二次 → `取消失敗：狀態為 Cancelled 的訂單不可取消`
 - `cancel_order(194)`（Shipped）→ `取消失敗：狀態為 Shipped 的訂單不可取消`
 - `cancel_order(99999)`（不存在）→ `取消失敗：找不到指定的訂單`
 - 額外確認一件沒人叫我測的事：**第二次取消失敗之後，庫存還是 102，沒有再加一次**。`idempotentHint: false` 說的是「重複呼叫結果會不同」，但這裡的「不同」是第二次被拒絕，不是庫存被重複回補——service 的狀態檢查先擋掉了。如果當初把回補寫在工具裡而不是 service 裡，這一格就會變成 104。

5. 過程中真的踩到的坑
 - **`dotnet build` 失敗，錯誤是檔案被鎖住**：`OrderHub.Core.dll` 正被 `OrderHub.Mcp (9100)` 佔用——那就是 Claude Code 正在跑的 MCP server。要先 `taskkill //PID 9100 //F` 才編得動，編完再 `/mcp` reconnect 才載入新工具。
 - 這其實是活動 1 那條「驗證前先確認跑的是新 build」的 MCP 版：**改完工具程式碼，桌上那個 server 還是舊的**。差別在網站是我自己開的、看得到；MCP server 是 client 幫我開的，很容易忘記它存在。

6. 我會帶走的一句話
 - 工具只做轉接（5 行），規則全留在 service——好處在第 4 點那一格具體看得到：規則只有一份，所以「從網頁取消」和「從 agent 取消」不可能長出兩種行為。

---

## 附錄：值得留下的對話片段

（貼 1–2 段最有代表性的 prompt 與回應**摘要**——不用貼全文，重點是「我怎麼問」和「它怎麼答」。）

 - **片段 A（練習 3，質疑規格出處）**
  - 我問：「其實那個庫存門檻什麼作用？」之前也追問過「你去哪裡看到『近 30 天』？」
  - 它答（摘要）：直接引用 `documents/activities/activity-guideline.md` 練習 3 規格原文「近 30 天售出數量（從訂單明細統計，排除 Cancelled 訂單）」，並說明 PROCESS.md 檢查清單只是簡化版沒重複「
近 30 天」。
  - 心得：要求 agent **舉出出處**，比它口頭保證可靠。
  
 - **片段 B（練習 4，先計畫再改）**
  - 我問（摘要）：重構 `CreateOrderAsync` 的驗證邏輯，先給計畫、說清楚「改什麼、不改什麼」，確認範圍再動手。
  - 它答（摘要）：提出抽 `ValidateNewOrder` + `ValidateOrderLine` 兩個純方法、副作用留主流程、訊息與順序一字不改，並先問我重構範圍要多大才實作。
  - 心得：重構最怕「順手改多」，把「不改什麼」寫進計畫能框住範圍。