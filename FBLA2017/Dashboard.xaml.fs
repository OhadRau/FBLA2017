namespace FEC

module Dashboard =
    open FsXaml
    open System.Windows
    open System.Data.SQLite
    open System.Windows.Controls
    open System.Collections.ObjectModel

    open FEC.Data
    open FEC.Common

    type DashboardBase = XAML<"Dashboard.xaml">

    type Dashboard (conn : SQLiteConnection, mainWindow : Window) as self =
        inherit DashboardBase ()

        do self.DataContext <- self
           let mainView = mainWindow.FindName("MainView") :?> Frame
           let refresh (u : Control) _ =
               // Force refresh by changing the data context
               let context = u.DataContext
               u.DataContext <- null
               u.DataContext <- context
           ([self.EmployeeCountCount; self.EmployeeExpensesCount; self.DailyAttendanceCount; self.HourlyAttendanceCount] : Control list)
           |> List.map refresh
           |> List.iter mainView.Navigated.Add

        member self.AddEmployee =
            let add _ =
                let mainView = mainWindow.FindName("MainView") :?> Frame
                mainView.Content <- AddEmployeePage.create conn mainWindow self
            ClosureCommand add

        member self.ViewEmployees =
            let view _ =
                let mainView = mainWindow.FindName("MainView") :?> Frame
                mainView.Content <- EmployeeList.create conn mainWindow self
            ClosureCommand view

        member self.ViewAttendance =
            let view _ =
                let attendance = getAttendanceForWeek conn System.DateTime.Now
                let mainView = mainWindow.FindName("MainView") :?> Frame
                mainView.Content <- AttendancePage.create conn mainWindow attendance (getWeek ()) self
            ClosureCommand view

        member self.PeakDays =
            let graph _ = Reports.peakDays conn
            ClosureCommand graph

        member self.PeakHours =
            let graph _ = Reports.peakHours conn
            ClosureCommand graph

        member self.EmployeeCount
            with get () =
                (getEmployees conn).Count

        member self.EmployeeExpenses
            with get () =
                let employees = getEmployees conn
                let addDays week =
                    let asInt = function true -> 1 | false -> 0
                    asInt week.Sunday + asInt week.Monday + asInt week.Tuesday + asInt week.Wednesday + asInt week.Thursday + asInt week.Friday + asInt week.Saturday
                query { for e in employees do
                        sumBy (match e.Payment with
                               | Wage w -> w * (Seq.map addDays e.Schedule |> Seq.sum)
                               | Salary s -> s / 52 )}
        
        member self.DailyAttendance
            with get () =
                let attendance = getAttendanceForWeek conn (getWeek ())
                let getToday hour = match System.DateTime.Now.DayOfWeek with
                    | Sunday -> hour.Sunday
                    | Monday -> hour.Monday
                    | Tuesday -> hour.Tuesday
                    | Wednesday -> hour.Wednesday
                    | Thursday -> hour.Thursday
                    | Friday -> hour.Friday
                    | Saturday -> hour.Saturday
                Array.map getToday attendance |> Array.sum

        member self.HourlyAttendance
            with get () =
                let attendance = getAttendanceForWeek conn (getWeek ())
                let hour = attendance.[System.DateTime.Now.Hour]
                match System.DateTime.Now.DayOfWeek with
                    | Sunday -> hour.Sunday
                    | Monday -> hour.Monday
                    | Tuesday -> hour.Tuesday
                    | Wednesday -> hour.Wednesday
                    | Thursday -> hour.Thursday
                    | Friday -> hour.Friday
                    | Saturday -> hour.Saturday
            and set count =
                let week = getWeek ()
                let attendance = getAttendanceForWeek conn week
                let hour = attendance.[System.DateTime.Now.Hour]
                match System.DateTime.Now.DayOfWeek with
                    | Sunday -> hour.Sunday <- count
                    | Monday -> hour.Monday <- count
                    | Tuesday -> hour.Tuesday <- count
                    | Wednesday -> hour.Wednesday <- count
                    | Thursday -> hour.Thursday <- count
                    | Friday -> hour.Friday <- count
                    | Saturday -> hour.Saturday <- count
                saveAttendanceForWeek conn week attendance

    let create conn window =
        let page = Dashboard (conn, window)
        page
