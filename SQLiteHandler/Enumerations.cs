namespace Vobsoft.Csharp.Database
{
    /// <summary>sets the behaviour of handling the connection to the database</summary>
    /// <remarks>
    /// "AllwaysOpen" opens the connection on instanciation of SqLiteHandler and closes it when disposing SqLiteHandler.
    /// "AutomaticOpenAndClose" automatically opens the connection when the user interacts with the databases and closes it when done.
    /// "Manually" The user has to manually open and close the connection when she/he wants to interact with the database.
    /// </remarks>
    public enum ConnectionBehaviour
    {
        AllwaysOpen,
        AutomaticOpenAndClose,
        Manually,
    }
}


