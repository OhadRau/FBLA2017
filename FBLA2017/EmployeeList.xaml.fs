namespace FEC

module EmployeeList =
    open FsXaml
    open System.Windows
    open System.Windows.Controls
    open System.Data.SQLite
    open System.Collections.ObjectModel

    open FEC.Data
    open FEC.Common

    type EmployeeListBase = XAML<"EmployeeList.xaml">

    type EmployeeList (conn : SQLiteConnection, mainWindow : System.Windows.Window) as self =
        inherit EmployeeListBase ()

        do self.DataContext <- self
           let mainView = mainWindow.FindName("MainView") :?> System.Windows.Controls.Frame
           let refresh _ =
               self.Rows <- getEmployees conn
               self.DataGrid.ItemsSource <- self.Rows
           mainView.Navigated.Add refresh

        let mutable employees = getEmployees conn

        member self.Rows with get () = employees and set value = employees <- value
        member self.GetEmployees = self.Rows

        member self.AddEmployee =
            let add _ =
                let mainView = mainWindow.FindName("MainView") :?> System.Windows.Controls.Frame
                mainView.Content <- FEC.AddEmployeePage.create conn mainWindow self
            ClosureCommand add

        member self.EditEmployee =
            let edit (param : obj) =
                let mainView = mainWindow.FindName("MainView") :?> System.Windows.Controls.Frame
                let employee = query {
                    for employee in self.Rows do
                        where (param :?> int = employee.ID)
                        select employee
                        exactlyOne }
                mainView.Content <- FEC.EditEmployeePage.create conn mainWindow self employee
            ClosureCommand edit

        member self.EditSchedule =
            let edit (param : obj) =
                let id = param :?> int
                let mainView = mainWindow.FindName("MainView") :?> System.Windows.Controls.Frame
                let schedule = query {
                    for employee in employees do
                        where (employee.ID = id)
                        select employee.Schedule
                        exactlyOne }
                mainView.Content <- FEC.SchedulePage.create conn mainWindow self id schedule
            ClosureCommand edit

        member self.DeleteEmployee =
            let delete (param : obj) =
                let id = param :?> int
                deleteEmployee conn id
                self.Rows <- getEmployees conn
                self.DataGrid.ItemsSource <- self.Rows
            ClosureCommand delete

    let create conn window employees =
        let page = EmployeeList (conn, window)
        page
