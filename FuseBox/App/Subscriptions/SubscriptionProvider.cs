namespace FuseBox.App.Subscriptions
{
    /// <summary>
    /// Identifies who owns the commercial subscription lifecycle.
    /// Internal is used for Free accounts and manual pre-billing test plans.
    /// Paddle is reserved for subscriptions synchronized from Paddle Billing.
    /// </summary>
    public enum SubscriptionProvider
    {
        Internal = 0,
        Paddle = 1
    }
}
