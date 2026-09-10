using System.Globalization;
using HomeBase.Database.Enums;
using HomeBase.Localization;

namespace HomeBase.Features.Common;

public static class Units
{
    public static string Format(decimal quantityBase, BaseUnit unit) =>
        unit switch
        {
            BaseUnit.Gram when Math.Abs(quantityBase) >= 1000 =>
                $"{Number(quantityBase / 1000)} {AppStrings.Get("Unit.Kilogram.Abbreviation")}",
            BaseUnit.Gram => $"{Number(quantityBase)} {AppStrings.Get("Unit.Gram.Abbreviation")}",
            BaseUnit.Milliliter when Math.Abs(quantityBase) >= 1000 =>
                $"{Number(quantityBase / 1000)} {AppStrings.Get("Unit.Liter.Abbreviation")}",
            BaseUnit.Milliliter =>
                $"{Number(quantityBase)} {AppStrings.Get("Unit.Milliliter.Abbreviation")}",
            _ => $"{Number(quantityBase)} {AppStrings.Get("Unit.Piece.Abbreviation")}",
        };

    public static string Abbreviation(BaseUnit unit) =>
        AppStrings.Get(
            unit switch
            {
                BaseUnit.Gram => "Unit.Gram.Abbreviation",
                BaseUnit.Milliliter => "Unit.Milliliter.Abbreviation",
                _ => "Unit.Piece.Abbreviation",
            }
        );

    public static string Describe(BaseUnit unit) =>
        AppStrings.Get(
            unit switch
            {
                BaseUnit.Gram => "Unit.Gram.Name",
                BaseUnit.Milliliter => "Unit.Milliliter.Name",
                _ => "Unit.Piece.Name",
            }
        );

    public static string Money(decimal amount) =>
        AppStrings.Format("Common.MoneyFormat", amount.ToString("N2", CultureInfo.CurrentCulture));

    private static string Number(decimal value) =>
        value.ToString("0.###", CultureInfo.CurrentCulture);
}
