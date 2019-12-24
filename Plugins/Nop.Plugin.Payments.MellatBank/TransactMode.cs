namespace Nop.Plugin.Payments.MellatBank
{
    /// <summary>
    /// Represents Mellat Bank payment processor transaction mode
    /// </summary>
    public enum TransactMode : int
    {
        /// <summary>
        /// Normal payment
        /// </summary>
        Normal = 1,
        /// <summary>
        /// Dynamic payment
        /// </summary>
        Dynamic = 2,
        /// <summary>
        /// Cumulative Dynamic payment
        /// </summary>
        CumulativeDynamic = 3
    }
}
