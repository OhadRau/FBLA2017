namespace FEC

module AttendancePage =
    open FsXaml
    open System.Windows
    open System.Data.SQLite
    open System.Windows.Controls
    open System.Collections.ObjectModel

    open FEC.Data
    open FEC.Common

    type MergedData = { // We can only have 1 data source for the DataGrid
        Label : string
        Hours : Hour<int>
    }

    type AttendancePageBase = XAML<"AttendancePage.xaml">

    type AttendancePage (conn : SQLiteConnection, mainWindow : Window, attendance : Hour<int>[]) as self =
        inherit AttendancePageBase ()

        do self.DataContext <- self

        let mutable merged =
            let labels =
                let hour_of_int i =
                    let hour = (i % 12) + 1
                    let period = if i < 12 then "AM" else "PM"
                    sprintf "%i:00 %s" hour period
                Array.map hour_of_int [|0..23|]
            Array.map2 (fun l h -> { Label=l; Hours=h }) labels attendance

        member val Rows = merged
        member self.GetAttendance = self.Rows |> Array.map (fun s -> s.Hours)

(*        member self.Save =
            let save _ =
                Data.saveSchedule conn id self.GetAttendance
                let mainView = mainWindow.FindName("MainView") :?> System.Windows.Controls.Frame
                mainView.Content <- employeeList
            ClosureCommand save

        member self.Cancel =
            let cancel _ =
                let mainView = mainWindow.FindName("MainView") :?> System.Windows.Controls.Frame
                mainView.Content <- employeeList
            ClosureCommand cancel *)

    let create conn window attendance =
        let page = AttendancePage (conn, window, attendance)
        page
