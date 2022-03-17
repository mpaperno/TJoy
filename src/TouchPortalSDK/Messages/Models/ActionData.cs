
namespace TouchPortalSDK.Messages.Models
{
    // the only real reason this is a custom type is for the JSON parser so we can write a custom handler
    // to break up the action data array into a dictionary.
    public sealed class ActionData : System.Collections.Generic.Dictionary<string, string>
    {

    }

}
