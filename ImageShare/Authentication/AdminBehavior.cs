using Mediator;

namespace ImageShare.Authentication;

internal sealed class AdminBehavior<TMessage, TResponse>(IUser user) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private static readonly bool RequiresAdmin = typeof(TMessage).IsDefined(typeof(RequireAdminAttribute), true);

    public ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (RequiresAdmin)
        {
            user.EnsureAdmin();
        }

        return next(message, cancellationToken);
    }
}
