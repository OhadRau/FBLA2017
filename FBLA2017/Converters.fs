// Must be a namespace, not a module, so that it can be called into from WPF
namespace FEC.Converters

open FEC.Data

// Provide the Converters used as StaticResources in WPF data bindings

type JobConverter () = // Convert a Job object to a string
    interface System.Windows.Data.IValueConverter with
        member self.Convert (value, _, _, _) = string_of_job (value :?> Job) :> obj
        member self.ConvertBack (_, _, _, _) = null

type PayConverter () = // Convert a Pay object to a string
    interface System.Windows.Data.IValueConverter with
        member self.Convert (value, _, _, _) = string_of_pay (value :?> Pay) :> obj
        member self.ConvertBack (_, _, _, _) = null

type MoneyConverter () = // Convert an integer representing money to a string
    interface System.Windows.Data.IValueConverter with
        member self.Convert (value, _, _, _) = pay_string_of_int (value :?> int) :> obj
        member self.ConvertBack (_, _, _, _) = null