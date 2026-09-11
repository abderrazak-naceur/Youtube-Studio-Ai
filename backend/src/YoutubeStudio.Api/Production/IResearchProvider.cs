namespace YoutubeStudio.Api.Production;

public interface IResearchProvider
{
    Task<ResearchResult> ResearchAsync(ResearchRequest request, CancellationToken cancellationToken);
}

public sealed record ResearchRequest(string Prompt);
public sealed record ResearchResult(IReadOnlyList<ResearchSource> Sources, IReadOnlyList<ResearchClaim> Claims);
public sealed record ResearchSource(string Url, string Title);
public sealed record ResearchClaim(string Text, string VerificationState);
