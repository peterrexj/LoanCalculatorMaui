#!/usr/bin/env python3
"""Builds guides/loan-affordability-calculator.html in the pages repo from the STEPS below.
Edit the text here, then:  python3 make_guide_page.py"""
import html, os
OUT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", "..",
        "YourSimpleAppsPages", "guides", "loan-affordability-calculator.html"))
G = "../images/loanaffordcalc/guide/"

def fig(img, cap, alt=None):
    return (f'<figure class="g-fig"><img src="{G}{img}" alt="{html.escape(alt or cap)}" loading="lazy" width="270">'
            f'<figcaption>{cap}</figcaption></figure>')
def row(*figs): return '<div class="g-row">' + "".join(figs) + "</div>"
def pdf(n, cap):
    return (f'<figure class="g-pdf"><img src="{G}pdf-page-{n:02d}.webp" alt="{html.escape(cap)}" loading="lazy">'
            f'<figcaption>{cap}</figcaption></figure>')

def purpose(t): return f'<div class="g-purpose"><b>Why this step matters</b><p>{t}</p></div>'
def calc(t):    return f'<div class="g-calc"><b>How it is worked out</b><p>{t}</p></div>'
def watch(t):   return f'<div class="g-watch"><b>Watch for</b><p>{t}</p></div>'
def split(f, text): return f'<div class="g-split">{f}<div class="g-text">{text}</div></div>'

def scenario(num, title, img, what, how, figures, helps, watch_for):
    figs = "".join(f"<li><b>{k}</b> – {v}</li>" for k, v in figures)
    return split(fig(img, title),
        f"<h3>{num}. {title}</h3><p><b>What it is.</b> {what}</p><p><b>How to use it.</b> {how}</p>"
        f"<p><b>The figures</b></p><ul>{figs}</ul>"
        f'<div class="g-calc"><b>How it helps your calculation</b><p>{helps}</p></div>'
        f'<div class="g-watch"><b>Watch for</b><p>{watch_for}</p></div>')

SECTIONS = [
("quick-start", "Quick start: your first answer in 2 minutes", f"""
<p>You only need three numbers to get a first answer: what the asset costs, how much you have for a deposit,
and your monthly income and spending. Everything else in the app makes the answer more accurate or lets you explore it.</p>
<ol class="g-steps">
<li><b>Accept the notice.</b> The first time you open the app it shows a short disclaimer. The calculator gives
estimates, not financial advice. Tap <b>Accept</b>.</li>
<li><b>Tap the ⚡ button on the Loan tab</b> and type the asset value and your deposit. The loan amount fills in by itself.</li>
<li><b>Go to Budget</b> and add your income and your regular spending.</li>
<li><b>Open What If.</b> The <b>Answer</b> tab tells you, in one line, whether it is affordable and how much you would have left each month.</li>
</ol>
{row(fig("01-first-run-disclaimer.webp","1. The first-run notice"), fig("04-quick-setup-filled.webp","2. Asset and deposit"), fig("20-expense-list.webp","3. Income and spending"), fig("25-whatif-answer.webp","4. The answer"))}
<p class="g-note">The examples in this guide use one household: a <b>$650,000</b> asset, <b>$130,000</b> deposit,
a 30-year loan at 5.5%, income of <b>$10,150</b> a month and spending of <b>$3,065</b> a month. Your figures will differ.
Screens are captured step by step, so a figure can change as more is entered; the text says when.</p>
<h3>The big picture</h3>
<p>The app answers one question in four layers. Each tab is one layer, and each feeds the next:</p>
<ol class="g-steps">
<li><b>Loan</b> works out what you would <i>borrow</i> and what it would <i>cost to repay</i>.</li>
<li><b>Budget</b> records what you <i>earn</i>, <i>spend</i>, and what the asset will cost to <i>keep</i>.</li>
<li><b>What If</b> compares the two: is the repayment <i>affordable</i>, and what would change that?</li>
<li><b>Insights and the PDF</b> turn all of it into a summary you can keep or share.</li>
</ol>
"""),
("loan", "1. The Loan tab: what you would borrow and repay", f"""
{purpose("Everything else in the app starts here. The loan is the single biggest commitment in the picture, so these figures drive the repayment, the affordability verdict and every scenario. Getting them right first makes everything after it right.")}
<h3>Step 1: enter the asset value and deposit</h3>
<p>Tap the ⚡ button (or tap any of the asset, deposit or loan figures). The <b>Loan Details</b> page opens. This is the only
place you type these three figures; elsewhere they are shown but not edited.</p>
{row(fig("02-loan-empty.webp","The Loan tab before anything is entered"), fig("03-quick-setup-empty.webp","Loan Details, empty"), fig("04-quick-setup-filled.webp","Asset $650,000, deposit $130,000"))}
<p>The three figures are linked: <b>asset value = deposit + loan</b>. Change the deposit and the loan moves; change the loan and the
deposit moves. Whichever you edited last wins. Each field also spells the number out in words underneath, so a stray zero is easy to spot. Tap <b>Done</b> when you are finished.</p>
<p>Fields for upfront costs, running cost, income and expenses appear on this page while they are still empty, so a new user can fill everything in one go.
Once a value exists they disappear, and you edit them where they live (Upfront Costs on the Loan tab; income and expenses in Budget). A small note on the page tells you which values are already being used.</p>
<h3>Step 2: read the overview</h3>
<p>The three boxes at the top are the headline: the <b>repayment</b> (and how often), the <b>loan amount</b> with the deposit it assumes, and an
<b>affordability</b> box that stays as a prompt until you have recorded income and expenses, then shows your estimated monthly position.
Below them are the asset value, the <b>total asset amount</b> (asset plus upfront costs), and a slider for the deposit.</p>
{row(fig("05-loan-overview.webp","Loan overview"), fig("06-loan-rate-term-frequency.webp","Rate, term and repayment frequency"))}
{calc("The repayment is a standard amortising-loan figure: a fixed payment, set from the loan amount, the interest rate for each period and the number of periods, so that the loan is exactly paid off at the end of the term. Each payment is split: first the interest on what you still owe, and the rest reduces the loan. Because the balance falls, the interest part shrinks and the principal part grows over time.")}
<h3>Step 3: set the interest rate, term and frequency</h3>
<ul>
<li><b>Interest Rate</b> – tap − / + to move in small steps, or tap the figure and type an exact rate. The step size is set in Settings. A rate that is even half a percent out changes the repayment noticeably, so use the rate you have actually been quoted.</li>
<li><b>Loan Term</b> – the number of years. A longer term lowers each repayment but increases the total interest.</li>
<li><b>Monthly / Fortnightly / Weekly</b> – how often you repay. The repayment box switches to that frequency immediately.</li>
</ul>
{row(fig("07-loan-rate-changed.webp","Rate set to 5.5%: $2,952 a month on a $520,000 loan"), fig("08-loan-fortnightly.webp","Switched to fortnightly: $1,489 each, with upfront costs included"))}
<p>Fortnightly and weekly repayments are shown as the amount you hand over <i>each time</i>, so they are smaller than the monthly figure. The What If tab shows what paying this way is really worth.</p>
<h3>Step 4: add upfront costs</h3>
{purpose("Buying costs more than the price tag: legal fees, bank fees, inspections and other one-off items. Leaving them out understates the loan. The app adds them to the loan, not to your deposit, so the deposit stays equal to the money you actually have.")}
<p>Open <b>Upfront Costs</b> and fill in whatever applies. Here: conveyancing $1,800, bank fees $600, inspection $450 and other $2,000, which totals <b>$4,850</b>.</p>
{row(fig("09-upfront-costs-empty.webp","Upfront costs, empty"), fig("10-upfront-costs-filled.webp","$4,850 in total"), fig("11-loan-with-upfront.webp","The loan is now $524,850 and the repayment $2,980"))}
{calc("Total asset amount = asset value + upfront costs ($650,000 + $4,850 = $654,850). Loan = total asset amount − deposit ($654,850 − $130,000 = $524,850). The repayment is then recalculated on the larger loan. Raising a cost always grows the loan and leaves your deposit alone.")}
<h3>Step 5: look at the repayment schedule</h3>
{purpose("It shows where your money actually goes, year by year. Most people are surprised by how much of the early repayments is interest. Seeing it is what makes the extra-repayment and rate scenarios later in the guide make sense.")}
<p>Open <b>Repayment Schedule</b> for a chart and a year-by-year table: the interest and principal paid each year, and the balance still owed. Tap a row to expand the years.</p>
{row(fig("12-repayment-schedule.webp","Annual repayments and remaining balance"))}
<p>The bars are the repayments for each year, split into principal and interest; the line is the remaining balance. Early on the bars are mostly interest; the crossover marks where the loan starts to fall faster.
Tap the <b>Disclaimer</b> bar at the top of the tab at any time to re-read the notice.</p>
{row(fig("44-loan-disclaimer-open.webp","The disclaimer, expanded"))}
"""),
("budget", "2. The Budget tab: what comes in, what goes out", f"""
{purpose("A repayment on its own means nothing. $2,980 a month is easy for one household and impossible for another. The Budget tab records the other half of the picture, so the app can tell you what is left over after the loan.")}
<p>Budget has four sub-tabs: <b>Income</b>, <b>Expenses</b>, <b>Asset costs</b> and <b>Summary</b>. Entries save automatically, and the other tabs pick them up the next time you open them.</p>
<h3>Income</h3>
<p>Tap the round <b>+</b> button. Give the income a <b>name</b>, an <b>amount</b> and how often you receive it: weekly, fortnightly, monthly or yearly. The app converts each source to a monthly and a yearly figure, so sources paid on different schedules can be added together fairly.
Tap a row later to edit or delete it.</p>
{row(fig("13-budget-income-empty.webp","Income, empty"), fig("14-add-income-form.webp","Add income"), fig("15-add-income-filled.webp","Salary, $7,800 monthly = $93,600 a year"), fig("16-income-list.webp","Three sources: $10,150 a month"))}
<p>Use the amount you actually receive. Take-home pay gives a more honest answer than gross pay. The three boxes at the top show monthly income, yearly income and monthly expenses; they recalculate as you add and remove rows.</p>
<h3>The two switches</h3>
<p>The switches at the top decide what "left over" means:</p>
<ul>
<li><b>Take off my everyday spending</b> – income minus your regular spending: what remains once the bills are paid.</li>
<li><b>Also take off the loan and its running costs</b> – the same, minus the repayment and asset costs: what remains once you are paying for the asset.</li>
</ul>
<p>Tap the ⓘ for a longer explanation in the app.</p>
{row(fig("45-budget-income-explainer.webp","The explanation panel"))}
<h3>Expenses</h3>
<p>The same idea, with ready-made categories (Groceries, Utilities, Transport, Insurance, Childcare and more). Add an amount to the categories that apply; the rest stay at $0. A category can hold more than one entry, so a person can list several insurance policies separately.</p>
{row(fig("17-expenses-empty.webp","Expenses, empty"), fig("18-add-expense-form.webp","Add expense"), fig("19-add-expense-filled.webp","Groceries, $950 a month"), fig("20-expense-list.webp","Spending of $3,065 a month"))}
<p>Be realistic here. The affordability answer is only as honest as your spending figures, and the stress test in What If depends on them directly. Occasional costs (annual insurance, holidays) can be entered with a yearly frequency; the app turns them into a monthly share.</p>
<h3>Asset costs</h3>
{purpose("Owning an asset costs money after you buy it: rates and taxes, maintenance, insurance, running costs. These are <i>ongoing</i>, unlike upfront costs, and they add to the repayment as a real monthly cost of ownership.")}
<p>Tap <b>+</b>, name the cost, give an amount and a frequency. A yearly cost is converted to a monthly share, and a weekly cost to a monthly equivalent.</p>
{row(fig("21-asset-costs-empty.webp","Asset costs, empty"), fig("22-asset-cost-entry.webp","Add an asset cost"), fig("22b-asset-cost-filled.webp","Council charge, $1,500 a year"), fig("23-asset-costs-filled.webp","Two costs: $2,292 a month in total"))}
<p class="g-note">The asset costs shown above were added afterwards as an example, so most other screens in this guide were captured before them and show $0 asset costs. When you enter them, your <b>monthly position</b> falls by the same amount (compare the £ screen later, where the position is lower because of them).</p>
{calc("Monthly cost of owning = loan repayment + asset costs (each converted to a monthly amount). A $1,500 yearly cost counts as $125 a month; a $500 weekly cost counts as about $2,167 a month.")}
<h3>Summary</h3>
<p>One page that brings it together: money in, money out and what is left, with a doughnut chart and a breakdown by category. It is the quickest way to sanity-check your entries.</p>
{row(fig("24-budget-summary.webp","Net $7,085 a month before the loan; $4,105 after it"))}
{calc("Net every month = income − expenses ($10,150 − $3,065 = $7,085). After the loan = net − repayment ($7,085 − $2,980 = $4,105). The percentage shown is the repayment as a share of your income.")}
"""),
("whatif", "3. The What If tab: is it affordable, and what would change that?", f"""
{purpose("This is where the loan and the budget meet. The tab turns your figures into a verdict, then lets you test the plan: what if rates rise, what if you pay extra, what if you borrow less. Learning how your loan behaves under change is more valuable than the single number it starts with.")}
<h3>Answer</h3>
<p>The verdict in one box (for example <b>Affordable, with room to spare</b>) and the amount left each month after the repayment, running costs and spending. Below it: the repayment, your net income, and the repayment as a share of income. Lenders usually look at that last figure.</p>
{row(fig("25-whatif-answer.webp","The Answer tab"), fig("46-whatif-understanding-numbers.webp","“Understanding these numbers” explains each figure"))}
{calc("Monthly spare = income − expenses − repayment − asset running costs. The verdict compares that spare amount, and the repayment's share of income, against rules of thumb. A very high share, or a spare amount near zero, moves the verdict from <i>Affordable</i> towards <i>Tight</i> or <i>Stretched</i>. It is an estimate based on your entries, not a lender's decision.")}
<h2 id="scenarios">The nine scenarios</h2>
<p>Each scenario changes one part of your plan and shows the effect, side by side with where you are today. Tap one to open it; each has controls at the top, results below, and an <b>Understanding this scenario</b> panel in the app that explains the same points in more detail.</p>
{row(fig("26-whatif-scenarios.webp","The nine scenarios, each with its headline effect"))}
{scenario(1,"Rate change","27-scenario-rate-change.webp",
 "Interest rates move, and your rate is the one number on a loan you do not control. This shows what a rise or fall does to the repayment and to the total interest.",
 "Use − and + to dial in a change (for example +0.50%). Larger tests, such as +2% or +3%, are closer to what has happened historically than half a percent.",
 [("New rate","your rate plus the change"),("Monthly change","how much more (or less) you pay each month"),("New monthly","the repayment at the new rate"),("Total interest","interest over the whole term at the new rate"),("The chart","each bar is a year, split into principal and interest; the line is the balance still owed"),("The banner","how much room is left against your income and expenses")],
 "A +0.50% change on this loan lifts the rate to 6%, adds about <b>$167 a month</b> and takes total interest to about $607,977. A small monthly change becomes a large cost over 30 years, which is why it is worth seeing before you commit. The monthly change is the figure to compare with your spare money each month.",
 "Unless your rate is fixed, a lender can move it at any time, and a fixed period ends.")}
{scenario(2,"Extra repayment","28-scenario-extra-repayment.webp",
 "Paying a little more than you have to each month. Every dollar of the extra goes straight to the balance, so none of it is spread over interest.",
 "Set an extra monthly amount with − and +.",
 [("Extra per month","what you add on top of the required repayment"),("Time saved","how much sooner the loan is paid off"),("Interest saved","interest you will never be charged"),("The chart","two lines: what you owe at your current repayment, and with the extra. The widening gap is the extra compounding")],
 "An extra $500 a month on this loan finishes it about <b>8 years 7 months earlier</b> and saves around <b>$178,444</b> of interest. The saving appears because the lower balance is charged interest each month, so the benefit grows with every month the extra is paid.",
 "Check your lender accepts extra repayments without a fee; some fixed-rate loans cap or charge for them. Keep an emergency fund first: money paid onto a loan is much harder to get back.")}
{scenario(3,"Loan term","29-scenario-loan-term.webp",
 "The term sets a trade-off: a longer loan makes every month easier and the loan as a whole more expensive.",
 "Nothing to enter. Three terms are shown side by side, and the column marked YOUR TERM is the one you have now.",
 [("The three columns","the same loan over a different number of years"),("Monthly","what you hand over each month on that term"),("Interest","total interest over that term, the real price of the extra years")],
 "Compare the monthly figure against your spare income, and the interest figure against how much you want to pay overall. It shows exactly what you buy, and what you pay, for each extra five years.",
 "The cheapest total is not automatically the right answer; a repayment you cannot sustain is worse than a longer loan. Many lenders let you overpay a long loan, which gives a short loan's cost with a long loan's safety net.")}
{scenario(4,"Deposit","30-scenario-deposit.webp",
 "Your deposit decides how much you borrow, and so both the repayment and the total interest.",
 "Nothing to enter. Three deposit levels are shown as a share of the asset price, and YOUR DEPOSIT marks where you are now.",
 [("The three columns","different deposits, as a share of the asset price"),("Loan","what you would borrow with that deposit"),("Monthly","the repayment that loan implies")],
 "On this example, moving the deposit from 15% to 25% lowers the monthly repayment from about $3,137 to about $2,768. It shows what saving a little longer is worth, and what putting down less would cost every month afterwards.",
 "Many lenders price by loan-to-value band, so crossing a threshold (often 80% or 90%) can cut your rate as well. This screen cannot model that, because the bands depend on your lender.")}
{scenario(5,"Lump sum","31-scenario-lump-sum.webp",
 "A single one-off payment onto the loan: a bonus, an inheritance, the proceeds of a sale.",
 "Set the amount with − and +.",
 [("Amount","the one-off payment you are modelling"),("Time saved","how much sooner the loan finishes"),("Interest saved","interest you will never be charged"),("New loan balance","what you owe right after the payment"),("The chart","what you owe with and without the lump sum. The two lines never meet again")],
 "A $10,000 lump sum saves about <b>1 year 4 months</b> and roughly <b>$37,681</b> of interest on this loan, and leaves a balance of about $514,850. The same amount paid early is worth far more than paid late, because it reduces the balance that interest is charged on for every remaining year.",
 "Check for early-repayment charges, common on fixed-rate loans. Compare against what the money could earn elsewhere, and against clearing any higher-interest debt first.")}
{scenario(6,"Repayment frequency","32-scenario-repayment-frequency.webp",
 "Paying fortnightly or weekly instead of monthly shortens the loan without feeling like paying more.",
 "Nothing to enter. Monthly, fortnightly and weekly are shown side by side; YOUR FREQUENCY marks how you pay now.",
 [("Payment","what you hand over each time, not per month"),("Time saved and interest saved","both measured against paying monthly"),("The chart","one line per frequency, showing what you owe over time")],
 "A year holds 26 fortnights but only 12 months, so paying half the monthly amount every fortnight makes 13 monthly-equivalent payments a year instead of 12. That one extra payment goes entirely onto the balance. Weekly works the same way, so its saving is almost identical.",
 "The saving comes from the extra payment, not the frequency itself. A lender that splits your monthly payment into 24 fortnightly halves gives you no benefit; confirm yours takes half the monthly amount 26 times a year.")}
{scenario(7,"Offset account","33-scenario-offset.webp",
 "An offset account is a savings or transaction account linked to your loan. Interest is charged only on the difference between the loan balance and the offset balance.",
 "Set the balance you would keep in the account, and optionally the rate at which the lender applies it. Leave the rate matching your loan rate for a full offset.",
 [("Balance","how much you keep in the offset account"),("Rate","models a lender applying the offset at a different effective rate"),("Time saved","how much sooner the loan ends"),("Interest saved","interest never charged"),("Monthly interest saving","what the offset is worth this month at that balance"),("The chart","what you owe over time, with and without the offset")],
 "A 600,000 loan with 20,000 in offset is charged interest on 580,000. On this loan, keeping $20,000 in offset saves about <b>2 years 1 month</b> and <b>$74,501</b> of interest. Your repayment does not change, so more of each payment reaches the balance, and that is what shortens the term.",
 "An offset only helps while the money stays in the account. Weigh any account or package fee against the saving, and check whether the same money would earn more elsewhere after tax.")}
{scenario(8,"Stress test","34-scenario-stress-test.webp",
 "Instead of asking what a particular rate rise costs, this asks the question in reverse: how far could the rate rise before this loan stops working on the income and expenses you entered?",
 "Nothing to enter; it uses your Loan and Budget figures. Fill both in first.",
 [("Breaks even at","the estimated rate at which your repayment equals income minus expenses, leaving nothing over"),("Your buffer","the gap between that rate and your current rate"),("Current monthly surplus","income, minus expenses, minus the repayment you have now"),("The chart","your current rate beside the break-even rate"),("The banner","grades the buffer as comfortable, moderate or at risk")],
 "On this household the loan breaks even at about <b>16.06%</b>, a buffer of roughly <b>10.56%</b> above today's 5.5%, with a surplus of $4,105 a month. If you are still deciding how much to borrow, this is the most useful number on the page.",
 "It is only as good as your entries: understate your spending and the buffer looks larger than it is. It also assumes income holds steady, which is often not what happens when rates rise.")}
{scenario(9,"Combined strategy","35-scenario-combined.webp",
 "Real repayment plans stack several things: paying fortnightly, adding a little extra, keeping savings in offset. This lets you switch them on together.",
 "Tick the strategies you want (frequency, extra repayment, lump sum, offset) and set their amounts. Everything below updates.",
 [("The switches","turn each strategy on or off"),("Time saved","how much sooner the loan finishes with that combination"),("Interest saved","total interest avoided"),("Paid off in","the term the combination produces outright"),("The chart","your loan as it stands, against the combined strategy")],
 "The effects compound rather than simply add up. For this example, fortnightly payments with $500 extra, a $10,000 lump sum and $20,000 in offset together finish the loan in about <b>17 years 3 months</b> instead of 30, and save around <b>$285,015</b> of interest.",
 "Check each piece is something you could keep up for years, not just this month. A modest plan you maintain saves more than an ambitious one you abandon.")}
{row(fig("47-scenario-explainer.webp","Every scenario has an “Understanding this scenario” panel"))}
<h3>Projection</h3>
{purpose("A loan lasts decades, but a budget is only a snapshot. The projection looks ahead over ten years so you can see whether income and spending are likely to stay in step as your circumstances change.")}
<p>The chart shows projected income and expenses year by year, with a table beneath you can page through. Open <b>Adjust the settings</b> to set an annual growth rate for income and for spending, and choose whether to take off the loan and its costs.</p>
{row(fig("36-whatif-projection.webp","Income and expense projection"), fig("48-whatif-projection-settings.webp","Adjusting the growth rates"))}
{calc("Each year's figure = last year's figure × (1 + growth rate). With 0% growth the projection is flat. Try a small positive growth for income and a similar rate for expenses to model inflation.")}
<h3>Insights</h3>
<p>Plain-language summaries of your position in three groups: <b>Key insights</b> (affordability at a glance), <b>Asset insights</b> (purchase price, upfront costs, total) and <b>Loan insights</b> (what the bank is repaid in total, and the interest). It is also where the PDF export lives.</p>
{row(fig("37-whatif-insights.webp","The Insights tab"), fig("49-whatif-insights-panel.webp","An insights panel opened"))}
"""),
("export", "4. The PDF report: keep it, share it, bring it to the bank", f"""
{purpose("A lender, broker, partner or adviser will want the numbers, not the app. The PDF is a complete record of your plan: the figures you entered, the results, and the whole repayment schedule, in a document you can save, print or send.")}
<h3>How to export</h3>
<ol class="g-steps">
<li>Open <b>What If</b> and tap <b>Export Detailed Insights</b> (on the Insights tab, or the card at the bottom of Answer).</li>
<li>The app builds the report. This can take several seconds; a progress message shows while it works.</li>
<li>The standard iOS share sheet opens. Choose <b>Preview</b> to read it, <b>Save to Files</b> to keep it, or share it by mail or message.</li>
</ol>
{row(fig("41-export-card.webp","The export card"), fig("43-export-share-sheet.webp","Share sheet: Preview, Save to Files, More"))}
<h3>What is in the report</h3>
<p>A 13-page report for this example. Every figure in it comes from what you entered, so check the Loan and Budget tabs are up to date first.</p>
<ol class="g-steps">
<li><b>Disclaimer</b> (pages 1 to 3) – the same notice you accepted on first run.</li>
<li><b>Key Insights</b> (page 4) – affordability, loan, total repayment, term payment, income and expenses at a glance.</li>
<li><b>Asset Insights</b> (page 5) – asset price, each upfront cost listed, and the total asset cost.</li>
<li><b>Loan</b> and <b>Amortisation</b> (pages 6 to 8) – loan amount, deposit, total repayment, total interest, rate, frequency, and the full year-by-year schedule.</li>
<li><b>Your Expenses</b> (pages 9 to 11) – repayments, asset costs and every spending category in weekly, fortnightly, monthly and yearly terms, with the share each takes.</li>
<li><b>Your Income</b> (pages 12 and 13) – each source, and the income left after expenses and the new loan.</li>
</ol>
<div class="g-pdfs">
{pdf(4,"Page 4 – Key insights")}{pdf(5,"Page 5 – Asset insights")}{pdf(6,"Page 6 – Loan details")}
{pdf(9,"Page 9 – Your expenses")}{pdf(12,"Page 12 – Your income")}
</div>
<p class="g-note">Exports include whatever is on the Budget tab at that moment. Review your entries for names or amounts you would not want to share before sending the file to anyone.</p>
"""),
("settings", "5. Settings: make the app yours", f"""
{purpose("Everyone reads numbers differently. Settings controls how the app looks and how fast the controls move, and it is where you can erase what the app has stored.")}
<h3>Theme, font, text size and currency</h3>
{row(fig("38-settings.webp","The Appearance section"), fig("51-settings-theme-picker.webp","Theme: Dark, Light, Forest, Warm"), fig("52-settings-font-picker.webp","Font: Lato, Nunito, Quicksand and more"), fig("53-settings-currency-picker.webp","Currency list"))}
<ul>
<li><b>Theme</b> – Dark, Light, Forest or Warm. Applies instantly everywhere.</li>
<li><b>Font</b> – eight typefaces, from clean sans-serif to serif and display styles.</li>
<li><b>Text size</b> – Small, Default, Large or XL, for easier reading.</li>
<li><b>Currency</b> – the symbol shown on every figure. It changes the symbol only; amounts are not converted.</li>
</ul>
{row(fig("54-loan-pound-currency.webp","The Loan tab in £ (with the asset costs from earlier included, so the monthly position is lower)"))}
<h3>Step sizes and defaults</h3>
<p>Set how far each − / + tap moves things: loan interest rate (0.05% by default), the what-if rate (0.25%), monthly repayment, lump sum and offset balance. Smaller steps give fine control; larger steps make big changes quick. <b>What If defaults</b> and <b>Calculator defaults</b> set the starting values for scenarios and the limits for the calculator.</p>
{row(fig("50-settings-step-sizes.webp","Step sizes"), fig("39-settings-support.webp","Support and purchases"))}
<h3>Support</h3>
<p>Re-read the <b>disclaimer</b> and <b>privacy policy</b>, share the app, send feedback, or <b>restore a purchase</b> if you have bought the full version on another device.</p>
<h3>Delete your data</h3>
<p>Your figures are stored only on your device. Under <b>Delete your data</b> you can remove loan, income or expense records, or everything, at any time. Deleting removes them from the screens straight away, so the Loan and What If tabs will no longer use them.</p>
{row(fig("40-settings-delete-data.webp","Delete your data"))}
"""),
("faq", "Good to know", """
<ul>
<li><b>Is my data uploaded anywhere?</b> No. It stays on your device.</li>
<li><b>Are the results financial advice?</b> No. They are estimates; a lender's figures will differ. Talk to a qualified adviser before you borrow.</li>
<li><b>Why does the loan include my upfront costs?</b> Costs are folded into the loan so your deposit stays equal to the money you actually have.</li>
<li><b>I changed my income and the answer did not change.</b> Open the What If tab again; it refreshes each time you arrive.</li>
<li><b>Why does the repayment on my lender's quote differ?</b> Lenders may compute interest daily, add fees, or round differently. Use the figures here to compare options, then confirm with your lender.</li>
<li><b>Where do I start if I only have five minutes?</b> Loan (asset and deposit), then Budget (income and spending), then the Answer tab.</li>
</ul>
"""),
]

toc = "".join(f'<li><a href="#{i}">{t}</a></li>' for i, t, _ in SECTIONS)
body = "".join(f'<section id="{i}"><h2>{t}</h2>{b}</section>' for i, t, b in SECTIONS)

PAGE = f"""<!DOCTYPE html>
<html lang="en" data-theme="light">
<head>
<meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>User Guide – Loan Affordability Calculator</title>
<meta name="description" content="A step-by-step guide to the Loan Affordability Calculator: loan setup, budget, what-if scenarios, projections and the PDF report.">
<link rel="icon" href="../images/favicon.svg" type="image/svg+xml">
<link rel="stylesheet" href="../css/styles.css"><link rel="stylesheet" href="../css/blog-post.css">
<link href="https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;700&family=Open+Sans:wght@300;400;600&display=swap" rel="stylesheet">
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
<style>
.g-toc{{background:rgba(127,127,127,.1);border-radius:12px;padding:14px 22px;margin:18px 0}}
.g-toc ol{{margin:6px 0 0 18px}}
section{{margin:42px 0}} section h2{{margin-bottom:12px}}
.g-row{{display:flex;flex-wrap:wrap;gap:18px;justify-content:center;margin:20px 0}}
.g-fig{{margin:0;width:230px;text-align:center}}
.g-fig img{{width:100%;height:auto;display:block}}
.g-fig figcaption,.g-pdf figcaption{{font-size:.85rem;opacity:.8;margin-top:6px}}
.g-pdfs{{display:flex;flex-wrap:wrap;gap:16px;justify-content:center}}
.g-pdf{{margin:0;width:200px;text-align:center}}
.g-pdf img{{width:100%;height:auto;border:1px solid rgba(127,127,127,.35);border-radius:4px;box-shadow:0 2px 8px rgba(0,0,0,.2)}}
.g-note{{background:rgba(127,127,127,.12);border-left:4px solid var(--primary-color);padding:10px 14px;border-radius:6px}}
.g-steps li{{margin:8px 0}}
.g-purpose,.g-calc,.g-watch{{border-radius:8px;padding:10px 14px;margin:14px 0;font-size:.95rem}}
.g-purpose p,.g-calc p,.g-watch p{{margin:4px 0 0}}
.g-purpose{{background:rgba(70,130,200,.12);border-left:4px solid #4682c8}}
.g-calc{{background:rgba(60,170,120,.12);border-left:4px solid #3caa78}}
.g-watch{{background:rgba(220,150,40,.14);border-left:4px solid #dc9628}}
.g-split{{display:flex;flex-wrap:wrap;gap:26px;align-items:flex-start;margin:34px 0;padding-top:20px;border-top:1px solid rgba(127,127,127,.25)}}
.g-split .g-fig{{flex:0 0 230px}}.g-split .g-text{{flex:1 1 320px;min-width:0}}
.g-split h3{{margin-top:0}}
</style>
</head>
<body>
<div class="bg-circle-left"></div><div class="bg-circle-right"></div>
<header class="header">
<a href="../index.html"><img src="../images/logo-icon.png" alt="Company Logo" class="header-logo"></a>
<h1 class="header-title"><a href="../index.html" style="color:inherit;text-decoration:none;">User Guide</a></h1>
<div class="header-wave"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1440 70" preserveAspectRatio="none"><path d="M0,0 L1440,0 C1440,0 1080,70 720,70 C360,70 0,0 0,0 Z"></path></svg></div>
<div class="theme-switcher"><button class="theme-toggle" id="themeToggle"><i class="fas fa-sun light-icon"></i><i class="fas fa-moon dark-icon"></i></button></div>
</header>
<div class="container"><main><div class="blog-post-container">
<a href="../app-details.html?id=loanaffordcalc" class="back-to-blog"><i class="fas fa-arrow-left"></i> Back to the app</a>
<article class="blog-post">
<header class="blog-post-header"><h1 class="blog-post-title">Loan Affordability Calculator: the complete user guide</h1></header>
<div class="blog-post-content">
<p>From your very first launch to the exported PDF report. Start with the two-minute quick start; the rest of the guide goes through every screen in detail. Screens shown are from the iPhone app; Android is the same.</p>
<nav class="g-toc"><b>In this guide</b><ol>{toc}</ol></nav>
{body}
<p class="g-note">Estimates only; this app does not provide financial advice.</p>
</div></article></div></main></div>
<script>
(function(){{var t=localStorage.getItem('app-theme')||'light';document.documentElement.setAttribute('data-theme',t);
document.getElementById('themeToggle').addEventListener('click',function(){{var n=document.documentElement.getAttribute('data-theme')==='dark'?'light':'dark';
document.documentElement.setAttribute('data-theme',n);localStorage.setItem('app-theme',n);}});}})();
</script>
</body></html>"""
os.makedirs(os.path.dirname(OUT), exist_ok=True)
open(OUT, "w").write(PAGE)
print("wrote", OUT, len(PAGE)//1024, "KB")
