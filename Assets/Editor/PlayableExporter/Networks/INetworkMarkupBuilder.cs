internal interface INetworkMarkupBuilder
{
    PlayableAdNetwork Network { get; }

    string BuildHeadMarkup(string escapedOrientation);

    string BuildTailScript();
}
