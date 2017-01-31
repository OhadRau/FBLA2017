namespace FEC

module Common = // Functions to be shared among all modules
    // Provides a type for WPF Command event handlers
    type ClosureCommand (f : obj -> unit) = // Take a function "f" to make into a Command
        let event = DelegateEvent<System.EventHandler> () // The event being handled
        interface System.Windows.Input.ICommand with // Override the ICommand interface
            [<CLIEvent>]
            member self.CanExecuteChanged = event.Publish // Boilerplate
            member self.CanExecute _ = true // Can always be executed
            member self.Execute param = f param // Pass the parameters to the Command to "f"