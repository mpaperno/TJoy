using System.Collections.Generic;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Interfaces
{
    public interface ITouchPortalDataMessage : ITouchPortalMessage
  {
    /// <summary>
    /// The id of the action or connector.
    /// </summary>
    string Id { get; set; }

    /// <summary>
    /// Data is name/value pairs of options the user has selected for this action.
    /// Ex. data1: dropdown1
    ///     data2: dropdown2
    /// </summary>
    IReadOnlyCollection<ActionDataSelected> Data { get; set; }

    /// <summary>
    /// Returns the value of the selected item in an action data field.
    /// This value can be null in some cases, and will be null if data field is miss written.
    /// </summary>
    /// <param name="dataId">the id of the datafield.</param>
    /// <returns>the value of the data field as string or null if not exists</returns>
    string GetValue(string dataId);

  }
}
