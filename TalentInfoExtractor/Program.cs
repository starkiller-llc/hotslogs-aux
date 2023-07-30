using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Antlr4.Runtime;
using CASCExplorer;
using CascScraper;
using HeroInfoExtractor.Schema;
using HtmlAgilityPack;
using TalentInfoExtractor.Xml;

namespace TalentInfoExtractor {
    internal class Program {
        private const string OutputPath = @"..\..\..";
        private static readonly string BadChars = Regex.Escape("- '‘’:,!.\"”?()");

        private static readonly Scraper Scraper = new Scraper();
        private static readonly Dictionary<string, XmlDocument> ReferenceCatalog = new Dictionary<string, XmlDocument>();

        public static void Main(string[] args) {
            Scraper.FillTalentInfoList();
            GenerateHotsTimersFiles();
            SaveImages();
            GenerateOutputFiles();
            GenerateActorUnitFiles();
        }

        private static void GenerateHotsTimersFiles() {
            var basePath = Path.Combine(OutputPath, "HotsTimers");
            Directory.CreateDirectory(basePath);
            var arrayOfHero = new ArrayOfHero {
                Heros = new List<Hero>()
            };
            var talents = Scraper.TalentInfoList.ToLookup(x => x.HeroName);
            var skills = Scraper.SkillInfoList.ToLookup(x => x.HeroName);
            var heros = talents.Select(x => x.Key).ToList();
            var tierLevels = new[] { 1, 4, 7, 10, 13, 16, 20 };
            var tierLevelsChromie = new[] { 1, 2, 5, 8, 11, 14, 18 };
            foreach (var hero in heros) {
                var sanitizedHeroName = PrepareForImageUrl(hero);
                var imgDir = Path.Combine(basePath, "images", "heroes", sanitizedHeroName);
                Directory.CreateDirectory(imgDir);
                var skillsDir = Path.Combine(imgDir, "skills");
                Directory.CreateDirectory(skillsDir);
                var talentsDir = Path.Combine(imgDir, "talents");
                Directory.CreateDirectory(talentsDir);

                var xSkills = skills[hero]
                    // take only skills which are not active talents
                    .Where(x => talents[hero].All(y => y.TalentName != x.SkillName))
                    .Select(x => {
                        var sanitizedName = PrepareForImageUrl(x.SkillName);
                        var url = Uri.EscapeUriString(
                            $"https://enemycds.com/images/heroes/{sanitizedHeroName}/skills/{sanitizedName}.png");
                        return new Xml.Skill {
                            Name = x.SkillName,
                            Key = x.Key,
                            Cooldown = x.Cooldown,
                            Image = url,
                        };
                    }).ToList();
                skills[hero].ToList().ForEach(x => {
                    var ddsImage = new DDSImage(x.Image, true);
                    var sanitizedName = PrepareForImageUrl(x.SkillName);
                    var pngPath = Path.Combine(skillsDir, $"{sanitizedName}.png");
                    ddsImage.Save(pngPath);
                });
                var xTalents = talents[hero].Select(x => {
                    var sanitizedName = PrepareForImageUrl(x.TalentName);
                    var url = Uri.EscapeUriString(
                        $"https://enemycds.com/images/heroes/{sanitizedHeroName}/talents/{sanitizedName}.png");
                    return new Xml.Talent {
                        Name = x.TalentName,
                        Cooldown = skills[hero].FirstOrDefault(y => y.SkillName == x.TalentName)?.Cooldown,
                        TalentImage = url,
                        Tier = hero == "Chromie" ? tierLevelsChromie[x.Tier - 1] : tierLevels[x.Tier - 1],
                    };
                }).ToList();
                talents[hero].ToList().ForEach(x => {
                    var ddsImage = new DDSImage(x.Image, true);
                    var sanitizedName = PrepareForImageUrl(x.TalentName);
                    var pngPath = Path.Combine(talentsDir, $"{sanitizedName}.png");
                    ddsImage.Save(pngPath);
                });
                var portraitPath = Path.Combine(imgDir, "portrait.png");
                var ddsHeroImage = new DDSImage(Scraper.HeroImages[hero], true);
                var pngPathHero = Path.Combine(imgDir, "portrait.png");
                ddsHeroImage.Save(pngPathHero);
                var xHero = new Xml.Hero {
                    Name = hero,
                    Image = Uri.EscapeUriString($"https://enemycds.com/images/heroes/{sanitizedHeroName}/portrait.png"),
                    Skills = xSkills,
                    Talents = xTalents
                };
                arrayOfHero.Heros.Add(xHero);
            }

            var ser = new XmlSerializer(typeof(ArrayOfHero));
            using var f = new Utf8StringWriter();
            ser.Serialize(f, arrayOfHero);
            var hotstimersXml = f.ToString();

            var hotstimersFile = Path.Combine(basePath, "talents4.xml");
            if (File.Exists(hotstimersFile)) {
                File.Delete(hotstimersFile);
            }
            File.WriteAllText(hotstimersFile, hotstimersXml, Encoding.UTF8);
        }

        private static void GenerateActorUnitFiles() {
            var actorDir = Path.Combine(OutputPath, "ActorUnits");
            if (!Directory.Exists(actorDir)) {
                Directory.CreateDirectory(actorDir);
            }
            foreach (var actorUnit in Scraper.ActorUnits) {
                var unitName = actorUnit.Key;
                var pngPath0 = Path.Combine(actorDir, "0" + unitName + ".png");
                var pngPath1 = Path.Combine(actorDir, "1" + unitName + ".png");
                try {
                    var ddsImage = new DDSImage(actorUnit.Value.MinimapIcon, true);
                    ddsImage.Save(pngPath0);
                    ddsImage.Save(pngPath1);
                }
                catch (Exception e) {
                    File.WriteAllText(pngPath0 + "_error.txt", e.ToString());
                    File.WriteAllText(pngPath1 + "_error.txt", e.ToString());
                }
            }
        }

        private static void SaveImages() {
            var targetDir = Path.Combine(OutputPath, "TalentImages");
            if (!Directory.Exists(targetDir)) {
                Directory.CreateDirectory(targetDir);
            }
            foreach (var talentInfo in Scraper.TalentInfoList) {
                var ddsBytes = talentInfo.Image;
                var ddsImage = new DDSImage(ddsBytes, true);
                var talentName = talentInfo.TalentName;
                var heroName = talentInfo.HeroName;
                var rawFileName = $"{heroName}{talentName}";
                var imageName = $"{PrepareForImageUrl(rawFileName)}.png";
                var path = Path.Combine(targetDir, imageName);
                ddsImage.Save(path);
            }
        }

        private static string PrepareForImageUrl(string input) {
            return Regex.Replace(input, $"[{BadChars}]", "");
        }

        private static void GenerateOutputFiles() {
            var xmlText = Scraper.GenerateTalentInfoXml();
            var talentXmlFile = Path.Combine(OutputPath, "talentInfo.xml");
            if (File.Exists(talentXmlFile)) {
                File.Delete(talentXmlFile);
            }
            File.WriteAllText(talentXmlFile, xmlText);

            var csvText = Scraper.GenerateTalentInfoCsv();
            var talentCsvFile = Path.Combine(OutputPath, "talentInfo.csv");
            if (File.Exists(talentCsvFile)) {
                File.Delete(talentCsvFile);
            }
            File.WriteAllText(talentCsvFile, csvText);

            var sqlText = Scraper.GenerateTalentInfoSql();
            var sqlFileName = Path.Combine(OutputPath, "talentInfo.sql");
            if (File.Exists(sqlFileName)) {
                File.Delete(sqlFileName);
            }
            File.WriteAllText(sqlFileName, sqlText);
        }
    }
}
