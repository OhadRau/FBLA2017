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
           let refresh _ =
               // Force refresh by changing the data context
               let context = self.DailyAttendanceCount.DataContext
               self.DailyAttendanceCount.DataContext <- null
               self.DailyAttendanceCount.DataContext <- context

               let context = self.HourlyAttendanceCount.DataContext
               self.HourlyAttendanceCount.DataContext <- null
               self.HourlyAttendanceCount.DataContext <- context

           mainView.Navigated.Add refresh

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
            let graph _ = Reports.peakDays conn |> ignore
            ClosureCommand graph

        member self.PeakHours =
            let graph _ = Reports.peakHours conn |> ignore
            ClosureCommand graph

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
