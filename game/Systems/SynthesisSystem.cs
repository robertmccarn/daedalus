using Systemic.Engine.State;

public static class SynthesisSystem
{
    public static bool CanSynthesize(
        CampaignState campaign,
        Recipe recipe)
    {
        foreach (string ingredient in recipe.Ingredients)
        {
            int required = recipe.IngredientQuantities.TryGetValue(
                ingredient,
                out int quantity)
                ? quantity
                : 1;

            Material? material = campaign.Materials
                .FirstOrDefault(candidate => candidate.Id == ingredient);

            if (material == null || material.Quantity < required)
                return false;
        }

        return true;
    }

    public static Gear? SynthesizeGear(
        CampaignState campaign,
        Recipe recipe)
    {
        if (recipe.ResultKind != "Gear" ||
            !CanSynthesize(campaign, recipe))
            return null;

        foreach (string ingredient in recipe.Ingredients)
        {
            int required = recipe.IngredientQuantities.TryGetValue(
                ingredient,
                out int quantity)
                ? quantity
                : 1;

            Material material = campaign.Materials
                .First(candidate => candidate.Id == ingredient);

            material.Quantity -= required;

            if (material.Quantity == 0)
                campaign.Materials.Remove(material);
        }

        Gear gear = new()
        {
            Name = recipe.Name,
            Slot = recipe.ResultSlot,
            Power = recipe.ResultPower
        };

        campaign.Gear.Add(gear);
        return gear;
    }
}
