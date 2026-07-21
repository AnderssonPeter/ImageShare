using Mediator;

namespace ImageShare.Authentication;

internal sealed class AuthenticationBehavior<TMessage, TResponse>(IUser user) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private static readonly bool requiresAuthentication = typeof(TMessage).IsDefined(typeof(RequireAuthenticationAttribute), true);

    public ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (requiresAuthentication)
        {
            user.EnsureAuthenticated();
        }

        return next(message, cancellationToken);
    }
}
