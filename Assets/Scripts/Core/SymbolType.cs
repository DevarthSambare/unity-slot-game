namespace SlotGame.Core
{
    /// <summary>
    /// Every symbol that can land on a reel. Matches the 4 symbol sprites from the asset pack.
    /// Order doesn't mean anything, payouts live in the paytable.
    /// </summary>
    public enum SymbolType
    {
        Cherry = 0,
        Bell = 1,
        Bar = 2,
        Seven = 3
    }
}
