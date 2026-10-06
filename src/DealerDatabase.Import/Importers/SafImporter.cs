using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace DealerDatabase.Import.Importers;

/// <summary>
/// Imports SAF membership records and enriches matched dealer details.
/// </summary>
internal sealed class SafImporter(DealerImportSession session, ILogger logger)
{
    internal async Task ImportAsync(string dataDir, CancellationToken cancellationToken)
    {
        var path = Path.Combine(dataDir, "saf_members.xml");
        if (!File.Exists(path))
        {
            logger.LogWarning("SAF input not found: {Path}", path);
            return;
        }

        await using var stream = File.OpenRead(path);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        foreach (var member in document.Descendants().Where(element => element.Name.LocalName == "Member")
                     .OrderBy(element => (string?)element.Attribute("id"), StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var recordId = ImportParsing.EmptyToNull((string?)member.Attribute("id"));
            var legalName = ImportParsing.EmptyToNull(ImportParsing.GetElementValue(member, "LegalName")) ??
                ImportParsing.EmptyToNull(ImportParsing.GetElementValue(member, "Name"));
            if (recordId is null || legalName is null)
            {
                continue;
            }

            var rawName = ImportParsing.GetRawElementValue(member, "LegalName") ?? ImportParsing.GetRawElementValue(member, "Name");
            var rawPostcode = ImportParsing.GetRawElementValue(member, "Postcode");
            var postcode = Normalization.NormalizePostcode(rawPostcode);
            var dealer = session.FindBySourceKey("SAF", recordId) ?? session.MatchBySoftKey(legalName, postcode);
            dealer ??= session.CreateDealer(legalName);

            var rawStatus = ImportParsing.GetRawElementValue(member, "Status");
            var rawExpiry = ImportParsing.GetRawElementValue(member, "Expiry");
            var rawTradingAs = ImportParsing.GetRawElementValue(member, "TradingAs");
            var rawTelephone = ImportParsing.GetRawElementValue(member, "Telephone");
            var rawWebsite = ImportParsing.GetRawElementValue(member, "Website");
            var status = ImportParsing.EmptyToNull(rawStatus);
            var expiry = ImportParsing.ParseDate(rawExpiry);
            var tradingAs = ImportParsing.EmptyToNull(rawTradingAs);
            var telephone = Normalization.NormalizePhone(rawTelephone);
            var website = Normalization.NormalizeWebsite(rawWebsite);
            var rawAddress = ImportParsing.FormatRawAddress(
                ImportParsing.GetRawElementValue(member, "AddressLine1"),
                ImportParsing.GetRawElementValue(member, "AddressLine2"),
                ImportParsing.GetRawElementValue(member, "Town"),
                rawPostcode);
            var tradingAddress = ImportParsing.FormatAddress(
                ImportParsing.GetRawElementValue(member, "AddressLine1"),
                ImportParsing.GetRawElementValue(member, "AddressLine2"),
                ImportParsing.GetRawElementValue(member, "Town"),
                rawPostcode);

            dealer.SafStatus = status;
            dealer.SafExpiry = expiry;
            dealer.TradingName = tradingAs ?? dealer.TradingName;
            dealer.Postcode ??= postcode;
            dealer.Telephone ??= telephone;
            dealer.Website ??= website;
            dealer.TradingAddress ??= tradingAddress;
            var sourceRecord = session.AddSourceRecord(dealer, "SAF", recordId,
                ImportParsing.SourceField("Name", rawName, Normalization.NormalizeName(legalName)),
                ImportParsing.SourceField("TradingAs", rawTradingAs, Normalization.NormalizeName(tradingAs), "TradingName", isCurrentValue: tradingAs is not null && dealer.TradingName == tradingAs),
                ImportParsing.SourceField("Postcode", rawPostcode, postcode, "Postcode", isCurrentValue: postcode is not null && dealer.Postcode == postcode),
                ImportParsing.SourceField("Telephone", rawTelephone, telephone, "Telephone", isCurrentValue: telephone is not null && dealer.Telephone == telephone),
                ImportParsing.SourceField("Website", rawWebsite, website, "Website", isCurrentValue: website is not null && dealer.Website == website),
                ImportParsing.SourceField("Address", rawAddress, tradingAddress, "TradingAddress", isCurrentValue: tradingAddress is not null && dealer.TradingAddress == tradingAddress),
                ImportParsing.SourceField("Status", rawStatus, status, "SafStatus", isCurrentValue: status is not null),
                ImportParsing.SourceField("Expiry", rawExpiry, expiry, "SafExpiry", isCurrentValue: expiry.HasValue));

            session.AddNameObservation(dealer, sourceRecord, "Source", "Name", 0, rawName);
            session.AddNameObservation(dealer, sourceRecord, "Trading", "TradingAs", 0, rawTradingAs,
                isPrimary: tradingAs is not null && dealer.TradingName == tradingAs);
            session.AddAddressObservation(dealer, sourceRecord, "Trading", "Address", 0, rawAddress, tradingAddress,
                isPrimary: tradingAddress is not null && dealer.TradingAddress == tradingAddress);
        }
    }
}