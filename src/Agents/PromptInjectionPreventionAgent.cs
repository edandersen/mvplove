using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using AgentGovernance;
using System.Text.Json;

public class PromptInjectionDetectionAgent : AIAgent
{
    private AIAgent wrapped;
    private GovernanceKernel kernel;

    public PromptInjectionDetectionAgent(AIAgent wrapped, GovernanceKernel governanceKernel)
    {
        this.wrapped = wrapped;
        this.kernel = governanceKernel;
    }

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
    {
        return wrapped.CreateSessionAsync(cancellationToken);
    }

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        return wrapped.DeserializeSessionAsync(serializedState, jsonSerializerOptions, cancellationToken);
    }

    protected override Task<AgentResponse> RunCoreAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        var messagesEvaluated = messages.ToList();

        foreach(var message in messagesEvaluated.Where(m => m.Role == ChatRole.User))
        {
            var result = kernel.InjectionDetector!.Detect(message.Text);
            if (result.IsInjection)
            {
                return Task.FromResult(new AgentResponse(new ChatResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, result.Explanation))));
            }
        }

        return wrapped.RunAsync(messagesEvaluated, session, options, cancellationToken);
    }

#pragma warning disable CS8425 // Async-iterator member has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
#pragma warning restore CS8425 // Async-iterator member has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
    {
        var messagesEvaluated = messages.ToList();
        var found = false;

        foreach(var message in messagesEvaluated.Where(m => m.Role == ChatRole.User))
        {
            var result = kernel.InjectionDetector!.Detect(message.Text);
            if (result.IsInjection)
            {
                yield return new AgentResponseUpdate(new ChatResponseUpdate(ChatRole.System, result.Explanation));
                found = true;
            }
        }

        if (!found)
        {
            await foreach(var response in wrapped.RunStreamingAsync(messagesEvaluated, session, options, cancellationToken))
            {
                yield return response;
            }
        }

    }

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        return wrapped.SerializeSessionAsync(session, jsonSerializerOptions, cancellationToken);
    }
}
