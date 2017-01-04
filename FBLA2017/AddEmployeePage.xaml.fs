namespace FEC

module AddEmployeePage =
    open FsXaml
    open System
    open System.Windows
    open System.Data.SQLite
    open System.Windows.Controls
    open System.Collections.ObjectModel

    open FEC.Data
    open FEC.Common

    type AddEmployeePageBase = XAML<"AddEmployeePage.xaml">

    type AddEmployeePage (conn : SQLiteConnection, mainWindow : Window, employeeList : Page) as self =
        inherit AddEmployeePageBase ()

        do self.DataContext <- self

        member self.Cancel =
            let cancel _ =
                let mainView = mainWindow.FindName("MainView") :?> System.Windows.Controls.Frame
                mainView.Content <- employeeList
            ClosureCommand cancel

        member self.Save =
            let save _ =
                let first = self.FirstName.Text
                let last = self.LastName.Text
                let job = self.JobTitle.Text |> job_of_string
                let pay = match self.PayType.Text with
                          | "Wage" -> Wage (int_of_pay_string self.Pay.Text)
                          | "Salary" -> Salary (int_of_pay_string self.Pay.Text)
                ignore (addEmployee conn first last job pay <| blankSchedule ())
                let mainView = mainWindow.FindName("MainView") :?> System.Windows.Controls.Frame
                mainView.Content <- employeeList
            ClosureCommand save

    let create conn window employeeList =
        let page = AddEmployeePage (conn, window, employeeList)
        page
