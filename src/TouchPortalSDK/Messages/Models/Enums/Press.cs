namespace TouchPortalSDK.Messages.Models.Enums
{
    public enum Press
    {
        /// <summary>
        /// Standard action tap/click.
        /// When this is not a on hold action.
        /// </summary>
        Tap,

        /// <summary>
        /// On Hold action, on end of press of a hold button.
        /// Usually triggers with same behaviour as Tap actions.
        /// </summary>
        Up,

        /// <summary>
        /// On Hold action, on start of press of a hold button.
        /// </summary>
        Down
    }
}
