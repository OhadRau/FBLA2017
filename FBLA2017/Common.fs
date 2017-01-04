namespace FEC

module Common =
    type ClosureCommand (f : obj -> unit) =
        let event = DelegateEvent<System.EventHandler> ()
        interface System.Windows.Input.ICommand with
            [<CLIEvent>]
            member self.CanExecuteChanged = event.Publish
            member self.CanExecute _ = true
            member self.Execute param = f param