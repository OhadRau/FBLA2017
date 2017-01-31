namespace FEC

module Main = // Main/start-up module for the program
    open FsXaml
    open System
    open System.Reflection
    open System.Windows
    open System.Windows.Controls
    open System.Windows.Markup

    open FEC.Data

    // Import the XAML definition of the main window's GUI
    type MainWindow = XAML<"MainWindow.xaml">

    [<STAThread>]
    [<EntryPoint>]
    let main argv =
        // Create or open the database and store a connection to it
        let conn = createOrOpen "./FEC.db"
        // Initialize the main window
        let mainWindow = MainWindow ()
        // Initialize the dashboard view for the main window using the database connection
        let page = FEC.Dashboard.create conn mainWindow
        // Set the page for the main window to be the dashboard view
        mainWindow.MainView.Content <- page
        // Create an application to display the window and save the success/failure value as "ret"
        let application = Application ()
        let ret = application.Run mainWindow

        // Disconnect from the database (free system resources)
        conn.Close ()
        // Return "ret" to indicate success/failure
        ret