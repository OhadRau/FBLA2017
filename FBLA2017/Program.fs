namespace FEC.Program

module Main =
    open FsXaml
    open System
    open System.Reflection
    open System.Windows
    open System.Windows.Controls
    open System.Windows.Markup

    open FEC.Data

    type MainWindow = XAML<"MainWindow.xaml">

    [<STAThread>]
    [<EntryPoint>]
    let main argv =
        let conn = createOrOpen "./FEC.db"

        let mainWindow = MainWindow ()
        let page = FEC.Dashboard.create conn mainWindow
        mainWindow.MainView.Content <- page
        let application = Application ()
        let ret = application.Run mainWindow

        conn.Close ()
        ret