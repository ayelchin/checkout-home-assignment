using System.Collections.Immutable;

namespace PaymentGateway.Api.Domain.Models;

/// <summary>
/// Current ISO 4217 payment currencies shown by XE on 2026-11-16. Names and minor-unit digits are
/// from the SIX ISO 4217 List One published on 2026-09-17. Cryptocurrency, historic, fund, metal,
/// and other non-payment codes are omitted.
/// Sources: https://www.xe.com/currency/ and https://www.six-group.com/en/products-services/financial-information/market-reference-data/data-standards.html
/// </summary>
public record Currency
{
    public string Code { get; }
    public string Name { get; }
    public int? MinorUnitDigits { get; }

    public Currency(string code, string name, int? minorUnitDigits)
    {
        Code = code;
        Name = name;
        MinorUnitDigits = minorUnitDigits;
    }

    public static readonly Currency UaeDirham = new("AED", "UAE Dirham", 2);
    public static readonly Currency Afghani = new("AFN", "Afghani", 2);
    public static readonly Currency Lek = new("ALL", "Lek", 2);
    public static readonly Currency ArmenianDram = new("AMD", "Armenian Dram", 2);
    public static readonly Currency Kwanza = new("AOA", "Kwanza", 2);
    public static readonly Currency ArgentinePeso = new("ARS", "Argentine Peso", 2);
    public static readonly Currency AustralianDollar = new("AUD", "Australian Dollar", 2);
    public static readonly Currency ArubanFlorin = new("AWG", "Aruban Florin", 2);
    public static readonly Currency AzerbaijanManat = new("AZN", "Azerbaijan Manat", 2);
    public static readonly Currency ConvertibleMark = new("BAM", "Convertible Mark", 2);
    public static readonly Currency BarbadosDollar = new("BBD", "Barbados Dollar", 2);
    public static readonly Currency Taka = new("BDT", "Taka", 2);
    public static readonly Currency BahrainiDinar = new("BHD", "Bahraini Dinar", 3);
    public static readonly Currency BurundiFranc = new("BIF", "Burundi Franc", 0);
    public static readonly Currency BermudianDollar = new("BMD", "Bermudian Dollar", 2);
    public static readonly Currency BruneiDollar = new("BND", "Brunei Dollar", 2);
    public static readonly Currency Boliviano = new("BOB", "Boliviano", 2);
    public static readonly Currency BrazilianReal = new("BRL", "Brazilian Real", 2);
    public static readonly Currency BahamianDollar = new("BSD", "Bahamian Dollar", 2);
    public static readonly Currency Ngultrum = new("BTN", "Ngultrum", 2);
    public static readonly Currency Pula = new("BWP", "Pula", 2);
    public static readonly Currency BelizeDollar = new("BZD", "Belize Dollar", 2);
    public static readonly Currency CanadianDollar = new("CAD", "Canadian Dollar", 2);
    public static readonly Currency CongoleseFranc = new("CDF", "Congolese Franc", 2);
    public static readonly Currency SwissFranc = new("CHF", "Swiss Franc", 2);
    public static readonly Currency ChileanPeso = new("CLP", "Chilean Peso", 0);
    public static readonly Currency YuanRenminbi = new("CNY", "Yuan Renminbi", 2);
    public static readonly Currency ColombianPeso = new("COP", "Colombian Peso", 2);
    public static readonly Currency CostaRicanColon = new("CRC", "Costa Rican Colon", 2);
    public static readonly Currency CaboVerdeEscudo = new("CVE", "Cabo Verde Escudo", 2);
    public static readonly Currency CzechKoruna = new("CZK", "Czech Koruna", 2);
    public static readonly Currency DjiboutiFranc = new("DJF", "Djibouti Franc", 0);
    public static readonly Currency DanishKrone = new("DKK", "Danish Krone", 2);
    public static readonly Currency DominicanPeso = new("DOP", "Dominican Peso", 2);
    public static readonly Currency AlgerianDinar = new("DZD", "Algerian Dinar", 2);
    public static readonly Currency EgyptianPound = new("EGP", "Egyptian Pound", 2);
    public static readonly Currency Nakfa = new("ERN", "Nakfa", 2);
    public static readonly Currency EthiopianBirr = new("ETB", "Ethiopian Birr", 2);
    public static readonly Currency Euro = new("EUR", "Euro", 2);
    public static readonly Currency FijiDollar = new("FJD", "Fiji Dollar", 2);
    public static readonly Currency FalklandIslandsPound = new("FKP", "Falkland Islands Pound", 2);
    public static readonly Currency PoundSterling = new("GBP", "Pound Sterling", 2);
    public static readonly Currency Lari = new("GEL", "Lari", 2);
    public static readonly Currency GhanaCedi = new("GHS", "Ghana Cedi", 2);
    public static readonly Currency GibraltarPound = new("GIP", "Gibraltar Pound", 2);
    public static readonly Currency Dalasi = new("GMD", "Dalasi", 2);
    public static readonly Currency GuineanFranc = new("GNF", "Guinean Franc", 0);
    public static readonly Currency Quetzal = new("GTQ", "Quetzal", 2);
    public static readonly Currency GuyanaDollar = new("GYD", "Guyana Dollar", 2);
    public static readonly Currency HongKongDollar = new("HKD", "Hong Kong Dollar", 2);
    public static readonly Currency Lempira = new("HNL", "Lempira", 2);
    public static readonly Currency Gourde = new("HTG", "Gourde", 2);
    public static readonly Currency Forint = new("HUF", "Forint", 2);
    public static readonly Currency Rupiah = new("IDR", "Rupiah", 2);
    public static readonly Currency NewIsraeliSheqel = new("ILS", "New Israeli Sheqel", 2);
    public static readonly Currency IndianRupee = new("INR", "Indian Rupee", 2);
    public static readonly Currency IraqiDinar = new("IQD", "Iraqi Dinar", 3);
    public static readonly Currency IcelandKrona = new("ISK", "Iceland Krona", 0);
    public static readonly Currency JamaicanDollar = new("JMD", "Jamaican Dollar", 2);
    public static readonly Currency JordanianDinar = new("JOD", "Jordanian Dinar", 3);
    public static readonly Currency Yen = new("JPY", "Yen", 0);
    public static readonly Currency KenyanShilling = new("KES", "Kenyan Shilling", 2);
    public static readonly Currency Som = new("KGS", "Som", 2);
    public static readonly Currency Riel = new("KHR", "Riel", 2);
    public static readonly Currency ComorianFranc = new("KMF", "Comorian Franc", 0);
    public static readonly Currency Won = new("KRW", "Won", 0);
    public static readonly Currency KuwaitiDinar = new("KWD", "Kuwaiti Dinar", 3);
    public static readonly Currency CaymanIslandsDollar = new("KYD", "Cayman Islands Dollar", 2);
    public static readonly Currency Tenge = new("KZT", "Tenge", 2);
    public static readonly Currency LaoKip = new("LAK", "Lao Kip", 2);
    public static readonly Currency LebanesePound = new("LBP", "Lebanese Pound", 2);
    public static readonly Currency SriLankaRupee = new("LKR", "Sri Lanka Rupee", 2);
    public static readonly Currency LiberianDollar = new("LRD", "Liberian Dollar", 2);
    public static readonly Currency Loti = new("LSL", "Loti", 2);
    public static readonly Currency LibyanDinar = new("LYD", "Libyan Dinar", 3);
    public static readonly Currency MoroccanDirham = new("MAD", "Moroccan Dirham", 2);
    public static readonly Currency MoldovanLeu = new("MDL", "Moldovan Leu", 2);
    public static readonly Currency MalagasyAriary = new("MGA", "Malagasy Ariary", 2);
    public static readonly Currency Denar = new("MKD", "Denar", 2);
    public static readonly Currency Kyat = new("MMK", "Kyat", 2);
    public static readonly Currency Tugrik = new("MNT", "Tugrik", 2);
    public static readonly Currency Pataca = new("MOP", "Pataca", 2);
    public static readonly Currency Ouguiya = new("MRU", "Ouguiya", 2);
    public static readonly Currency MauritiusRupee = new("MUR", "Mauritius Rupee", 2);
    public static readonly Currency Rufiyaa = new("MVR", "Rufiyaa", 2);
    public static readonly Currency MalawiKwacha = new("MWK", "Malawi Kwacha", 2);
    public static readonly Currency MexicanPeso = new("MXN", "Mexican Peso", 2);
    public static readonly Currency MalaysianRinggit = new("MYR", "Malaysian Ringgit", 2);
    public static readonly Currency MozambiqueMetical = new("MZN", "Mozambique Metical", 2);
    public static readonly Currency NamibiaDollar = new("NAD", "Namibia Dollar", 2);
    public static readonly Currency Naira = new("NGN", "Naira", 2);
    public static readonly Currency CordobaOro = new("NIO", "Cordoba Oro", 2);
    public static readonly Currency NorwegianKrone = new("NOK", "Norwegian Krone", 2);
    public static readonly Currency NepaleseRupee = new("NPR", "Nepalese Rupee", 2);
    public static readonly Currency NewZealandDollar = new("NZD", "New Zealand Dollar", 2);
    public static readonly Currency RialOmani = new("OMR", "Rial Omani", 3);
    public static readonly Currency Balboa = new("PAB", "Balboa", 2);
    public static readonly Currency Sol = new("PEN", "Sol", 2);
    public static readonly Currency Kina = new("PGK", "Kina", 2);
    public static readonly Currency PhilippinePeso = new("PHP", "Philippine Peso", 2);
    public static readonly Currency PakistanRupee = new("PKR", "Pakistan Rupee", 2);
    public static readonly Currency Zloty = new("PLN", "Zloty", 2);
    public static readonly Currency Guarani = new("PYG", "Guarani", 0);
    public static readonly Currency QatariRial = new("QAR", "Qatari Rial", 2);
    public static readonly Currency RomanianLeu = new("RON", "Romanian Leu", 2);
    public static readonly Currency SerbianDinar = new("RSD", "Serbian Dinar", 2);
    public static readonly Currency RwandaFranc = new("RWF", "Rwanda Franc", 0);
    public static readonly Currency SaudiRiyal = new("SAR", "Saudi Riyal", 2);
    public static readonly Currency SolomonIslandsDollar = new("SBD", "Solomon Islands Dollar", 2);
    public static readonly Currency SeychellesRupee = new("SCR", "Seychelles Rupee", 2);
    public static readonly Currency SudanesePound = new("SDG", "Sudanese Pound", 2);
    public static readonly Currency SwedishKrona = new("SEK", "Swedish Krona", 2);
    public static readonly Currency SingaporeDollar = new("SGD", "Singapore Dollar", 2);
    public static readonly Currency SaintHelenaPound = new("SHP", "Saint Helena Pound", 2);
    public static readonly Currency Leone = new("SLE", "Leone", 2);
    public static readonly Currency SomaliShilling = new("SOS", "Somali Shilling", 2);
    public static readonly Currency SurinamDollar = new("SRD", "Surinam Dollar", 2);
    public static readonly Currency Dobra = new("STN", "Dobra", 2);
    public static readonly Currency ElSalvadorColon = new("SVC", "El Salvador Colon", 2);
    public static readonly Currency Lilangeni = new("SZL", "Lilangeni", 2);
    public static readonly Currency Baht = new("THB", "Baht", 2);
    public static readonly Currency Somoni = new("TJS", "Somoni", 2);
    public static readonly Currency TurkmenistanNewManat = new("TMT", "Turkmenistan New Manat", 2);
    public static readonly Currency TunisianDinar = new("TND", "Tunisian Dinar", 3);
    public static readonly Currency PaAnga = new("TOP", "Pa’anga", 2);
    public static readonly Currency TurkishLira = new("TRY", "Turkish Lira", 2);
    public static readonly Currency TrinidadAndTobagoDollar = new("TTD", "Trinidad and Tobago Dollar", 2);
    public static readonly Currency NewTaiwanDollar = new("TWD", "New Taiwan Dollar", 2);
    public static readonly Currency TanzanianShilling = new("TZS", "Tanzanian Shilling", 2);
    public static readonly Currency Hryvnia = new("UAH", "Hryvnia", 2);
    public static readonly Currency UgandaShilling = new("UGX", "Uganda Shilling", 0);
    public static readonly Currency UsDollar = new("USD", "US Dollar", 2);
    public static readonly Currency PesoUruguayo = new("UYU", "Peso Uruguayo", 2);
    public static readonly Currency UzbekistanSum = new("UZS", "Uzbekistan Sum", 2);
    public static readonly Currency BolivarSoberano = new("VES", "Bolívar Soberano", 2);
    public static readonly Currency Dong = new("VND", "Dong", 0);
    public static readonly Currency Vatu = new("VUV", "Vatu", 0);
    public static readonly Currency Tala = new("WST", "Tala", 2);
    public static readonly Currency CfaFrancBeac = new("XAF", "CFA Franc BEAC", 0);
    public static readonly Currency EastCaribbeanDollar = new("XCD", "East Caribbean Dollar", 2);
    public static readonly Currency CaribbeanGuilder = new("XCG", "Caribbean Guilder", 2);
    public static readonly Currency CfaFrancBceao = new("XOF", "CFA Franc BCEAO", 0);
    public static readonly Currency CfpFranc = new("XPF", "CFP Franc", 0);
    public static readonly Currency YemeniRial = new("YER", "Yemeni Rial", 2);
    public static readonly Currency Rand = new("ZAR", "Rand", 2);
    public static readonly Currency ZambianKwacha = new("ZMW", "Zambian Kwacha", 2);
    public static readonly Currency ZimbabweGold = new("ZWG", "Zimbabwe Gold", 2);

    public static ImmutableArray<Currency> Allowed { get; } = [UsDollar, PoundSterling, Euro];
}