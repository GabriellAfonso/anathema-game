#nullable enable
namespace Anathema.Client.Proof
{
    /// <summary>
    /// Um item da cobertura da partida 1: os 11 comandos, com o mulligan contado trocando e sem trocar
    /// (specs/005-presentation-facade/contracts/match-proof.md, passo 14).
    /// </summary>
    internal enum CoverageItem
    {
        MulliganSwapping,
        MulliganKeeping,
        PlayUnit,
        SpellWithTarget,
        SpellWithoutTarget,
        Pass,
        DeclareAttack,
        WithdrawAttacker,
        ConfirmAttack,
        AssignBlocker,
        RemoveBlocker,
        EndDefense,
    }
}
