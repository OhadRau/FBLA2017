namespace FEC

module SchedulePage =
    open FsXaml
    open System.Windows
    open System.Data.SQLite
    open System.Windows.Controls
    open System.Collections.ObjectModel

    open FEC.Data
    open FEC.Common

    type MergedData = { // We can only have 1 data source for the DataGrid
        Label : string
        Hours : Hour
    }

    type SchedulePageBase = XAML<"SchedulePage.xaml">

    type SchedulePage (conn : SQLiteConnection, mainWindow : Window, employeeList : Page, id : int, schedule : Hour[]) as self =
        inherit SchedulePageBase ()

        do self.DataContext <- self

        let mutable merged =
            let labels =
                let hour_of_int i =
                    let hour = (i % 12) + 1
                    let period = if i < 12 then "AM" else "PM"
                    sprintf "%i:00 %s" hour period
                Array.map hour_of_int [|0..23|]
            Array.map2 (fun l h -> { Label=l; Hours=h }) labels schedule

        member val Rows = merged
        member self.GetSchedule = self.Rows |> Array.map (fun s -> s.Hours)

        member self.Save =
            let save _ =
                Data.saveSchedule conn id self.GetSchedule
                let mainView = mainWindow.FindName("MainView") :?> System.Windows.Controls.Frame
                mainView.Content <- employeeList
            ClosureCommand save

        member self.Cancel =
            let cancel _ =
                let mainView = mainWindow.FindName("MainView") :?> System.Windows.Controls.Frame
                mainView.Content <- employeeList
            ClosureCommand cancel

    let create conn window employeeList id schedule =
        let page = SchedulePage (conn, window, employeeList, id, schedule)
        page
