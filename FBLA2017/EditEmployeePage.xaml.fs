namespace FEC

module EditEmployeePage =
    open FsXaml
    open System
    open System.Windows
    open System.Data.SQLite
    open System.Windows.Controls
    open System.Collections.ObjectModel

    open FEC.Data
    open FEC.Common

    type EditEmployeePageBase = XAML<"EditEmployeePage.xaml">

    type EditEmployeePage (conn : SQLiteConnection, mainWindow : Window, employeeList : Page, employee : Employee) as self =
        inherit EditEmployeePageBase ()

        do self.DataContext <- self

           self.FirstName.Text <- employee.FirstName
           self.LastName.Text <- employee.LastName
           let jobTitles = ["Manager"; "Custodian"; "Attraction Operator"; "Technician"; "Cook"; "Waiter/Waitress"]
           self.JobTitle.SelectedIndex <- List.findIndex (fun j -> j = string_of_job employee.JobTitle) jobTitles
           let payType, pay = match employee.Payment with
                              | Wage x -> 0, x
                              | Salary x -> 1, x
           self.PayType.SelectedIndex <- payType
           self.Pay.Text <- pay_string_of_int pay
           
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
                ignore (updateEmployee conn first last job pay)
                let mainView = mainWindow.FindName("MainView") :?> System.Windows.Controls.Frame
                mainView.Content <- employeeList
            ClosureCommand save

    let create conn window employeeList employee =
        let page = EditEmployeePage (conn, window, employeeList, employee)
        page
