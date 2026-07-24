using System;

namespace Core.Domain.Entities
{
    /// <summary>
    /// Durable idempotency ledger for processed Stripe webhook events, keyed by the
    /// Stripe event id (<c>evt_...</c>). Stripe delivers webhooks at-least-once and
    /// retries on any non-2xx response, so the same event can arrive multiple times.
    /// Recording each processed event id behind a unique index lets us detect and skip
    /// duplicates, keeping state-changing handlers (orders, escrow/refunds) from
    /// repeating or conflicting across retries, restarts and multiple API instances.
    /// </summary>
    public class StripeWebhookEvent : EntityBase
    {
        /// <summary>The Stripe event id (<c>evt_...</c>) — the idempotency key.</summary>
        public string EventId { get; set; }

        /// <summary>The Stripe event type, e.g. <c>payment_intent.succeeded</c>. Kept for auditing.</summary>
        public string EventType { get; set; }

        /// <summary>When the event was processed and recorded.</summary>
        public DateTime ProcessedAtUtc { get; set; }
    }
}
