using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Extensions;

namespace Core.Infrastructure.Persistence
{
    public class ApplicationDbContextSeed
    {
        public static async Task SeedAsync(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IApplicationDbContext dbContext
        )
        {
            await SeedRolesAndUsers(userManager, roleManager, configuration);
            await SeedGenders(dbContext);
            await SeedIndustries(dbContext);
            await SeedCurrencies(dbContext);
            await SeedCategories(dbContext);
            await ProductCategoriesSeed.SeedProductCategories(dbContext);
            await SeedPaymentMethods(dbContext);
            await SeedOnboardingPreferences(dbContext);
            await SeedVibes(dbContext);
        }

        private static async Task SeedGenders(IApplicationDbContext dbContext)
        {
            foreach (GenderEnum geEnum in Enum.GetValues(typeof(GenderEnum)))
            {
                if (!dbContext.Genders.Any(po => po.Key.ToLower() == geEnum.GetDisplayName().ToLower()))
                {
                    var gender = new Gender
                    {
                        Key = geEnum.GetDisplayName()
                    };

                    await dbContext.Genders.AddAsync(gender);
                }
            }

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        private static async Task SeedOnboardingPreferences(IApplicationDbContext dbContext)
        {
            try
            {
                if (!dbContext.OnboardingPreferences.Any())
                {
                    var female = await dbContext.Genders.SingleOrDefaultAsync(g =>
                        g.IsActive && g.Key.ToLower() == GenderEnum.Female.GetDisplayName().ToLower());
                    dbContext.OnboardingPreferences.AddRange(new List<OnboardingPreference>()
                    {
                        new OnboardingPreference
                            { Name = "Modest", Key = "Modest", Description = "Todo", Gender = female },
                        new OnboardingPreference
                        {
                            Name = "Casual", Key = "Casual", Description = "Simple and never overplanned",
                            Gender = female
                        },
                        new OnboardingPreference
                        {
                            Name = "Trendy", Key = "Trendy", Description = "The latest in fashion. Stay up-to-date",
                            Gender = female
                        },
                        new OnboardingPreference
                        {
                            Name = "Glam", Key = "Glam", Description = "Sassy and sexy yet elegant", Gender = female
                        },
                        new OnboardingPreference
                        {
                            Name = "Streetware", Key = "Streetware",
                            Description = "Active, comfortable and urban-inspired", Gender = female
                        },
                        new OnboardingPreference
                        {
                            Name = "Retro", Key = "Retro", Description = "Pre-loved pieces, fashionable again",
                            Gender = female
                        },
                        new OnboardingPreference
                            { Name = "Sexy", Key = "Sexy", Description = "Feminine and seductive", Gender = female },
                        new OnboardingPreference
                            { Name = "Business", Key = "Business", Description = "Todo", Gender = female },
                        new OnboardingPreference
                            { Name = "Sports", Key = "Sports", Description = "Todo", Gender = female },
                    });

                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                throw new Exception("An error occurred while seeding onboarding preferences", e);
            }
        }

        private static async Task SeedPaymentMethods(IApplicationDbContext dbContext)
        {
            try
            {
                if (!dbContext.PaymentMethods.Any())
                {
                    dbContext.PaymentMethods.AddRange(new List<PaymentMethod>()
                    {
                        new PaymentMethod()
                        {
                            Name = "Credit Card", Key = EnumHelper.ValueToString(PaymentMethodEnum.CreditCard),
                            Uid = Guid.NewGuid().ToString()
                        },
                        new PaymentMethod()
                        {
                            Name = "Cash on delivery", Key = EnumHelper.ValueToString(PaymentMethodEnum.CashOnDelivery),
                            Uid = Guid.NewGuid().ToString()
                        }
                    });

                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static async Task SeedCategories(IApplicationDbContext dbContext)
        {
            try
            {
                if (!dbContext.Categories.Any())
                {
                    await SeedMenCategories(dbContext);
                    await SeedWomenCategories(dbContext);

                    var categories = await dbContext.Categories.ToListAsync();

                    var closureData = CalculateClosureData(categories);

                    dbContext.CategoryClosures.AddRange(closureData);

                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static async Task SeedMenCategories(IApplicationDbContext dbContext)
        {
            //Mens
            var mensCategory = new Category { Name = "Men", Slug = "men", ParentCategoryId = null };
            dbContext.Categories.Add(mensCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var clothingCat = new Category { Name = "Clothing", Slug = "clothing", ParentCategoryId = mensCategory.Id };
            var shoesCat = new Category { Name = "Shoes", Slug = "shoes", ParentCategoryId = mensCategory.Id };
            var accessoriesCat = new Category
                { Name = "Accessories", Slug = "accessories", ParentCategoryId = mensCategory.Id };
            var faceBodyCat = new Category
                { Name = "Face & Body", Slug = "face-body", ParentCategoryId = mensCategory.Id };
            var fragrancesCat = new Category
                { Name = "Fragrances", Slug = "fragrances", ParentCategoryId = mensCategory.Id };
            dbContext.Categories.Add(clothingCat);
            dbContext.Categories.Add(shoesCat);
            dbContext.Categories.Add(accessoriesCat);
            dbContext.Categories.Add(faceBodyCat);
            dbContext.Categories.Add(fragrancesCat);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var topsCategory = new Category { Name = "Tops", Slug = "tops", ParentCategoryId = clothingCat.Id };
            var sweatersAndShirtsCategory = new Category
                { Name = "Sweaters & SweatShirts", Slug = "sweaters-sweatshirts", ParentCategoryId = clothingCat.Id };
            var jacketsCoatsCategory = new Category
                { Name = "Jackets & Coats", Slug = "jackets-coats", ParentCategoryId = clothingCat.Id };
            var bottomsCategory = new Category
                { Name = "Bottoms", Slug = "bottoms", ParentCategoryId = clothingCat.Id };
            var suitsAndFormalWearCategory = new Category
                { Name = "Suits & Formal Wear", Slug = "suits-formal-wear", ParentCategoryId = clothingCat.Id };
            var activeWearCategory = new Category
                { Name = "Active Wear", Slug = "active-wear", ParentCategoryId = clothingCat.Id };
            var otherCategory = new Category { Name = "Other", Slug = "other", ParentCategoryId = clothingCat.Id };
            dbContext.Categories.Add(topsCategory);
            dbContext.Categories.Add(sweatersAndShirtsCategory);
            dbContext.Categories.Add(jacketsCoatsCategory);
            dbContext.Categories.Add(bottomsCategory);
            dbContext.Categories.Add(suitsAndFormalWearCategory);
            dbContext.Categories.Add(activeWearCategory);
            dbContext.Categories.Add(otherCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var tShirtsCategory = new Category
                { Name = "T-Shirts", Slug = "t-shirts", ParentCategoryId = topsCategory.Id };
            var shirtsCategory = new Category { Name = "Shirts", Slug = "shirts", ParentCategoryId = topsCategory.Id };
            var poloShirtsCategory = new Category
                { Name = "Polo Shirts", Slug = "polo-shirts", ParentCategoryId = topsCategory.Id };
            var tankTopsCategory = new Category
                { Name = "Tank Tops", Slug = "tank-tops", ParentCategoryId = topsCategory.Id };
            dbContext.Categories.Add(tShirtsCategory);
            dbContext.Categories.Add(shirtsCategory);
            dbContext.Categories.Add(poloShirtsCategory);
            dbContext.Categories.Add(tankTopsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var shortSleeveCategory = new Category
                { Name = "Short Sleeve (Filter)", Slug = "short-sleeve-filter", ParentCategoryId = shirtsCategory.Id };
            var longSleeveCategory = new Category
                { Name = "Long Sleeve (Filter)", Slug = "long-sleeve-filter", ParentCategoryId = shirtsCategory.Id };
            dbContext.Categories.Add(shortSleeveCategory);
            dbContext.Categories.Add(longSleeveCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var sweatersCategory = new Category
                { Name = "Sweaters", Slug = "sweaters", ParentCategoryId = sweatersAndShirtsCategory.Id };
            var hoodiesCategory = new Category
                { Name = "Hoodies", Slug = "hoodies", ParentCategoryId = sweatersAndShirtsCategory.Id };
            var sweatshirtsCategory = new Category
                { Name = "Sweatshirts", Slug = "sweatshirts", ParentCategoryId = sweatersAndShirtsCategory.Id };
            dbContext.Categories.Add(sweatersCategory);
            dbContext.Categories.Add(hoodiesCategory);
            dbContext.Categories.Add(sweatshirtsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var allJacketsCategory = new Category
                { Name = "All Jackets", Slug = "all-jackets", ParentCategoryId = jacketsCoatsCategory.Id };
            var leatherJacketsCategory = new Category
            {
                Name = "Leather Jackets (Filter)", Slug = "leather-jackets-filter",
                ParentCategoryId = jacketsCoatsCategory.Id
            };
            var denimJacketsCategory = new Category
            {
                Name = "Denim Jackets (Filter)", Slug = "denim-jackets-filter",
                ParentCategoryId = jacketsCoatsCategory.Id
            };
            var coatsJacketsCategory = new Category
                { Name = "Coats", Slug = "all-jackets", ParentCategoryId = jacketsCoatsCategory.Id };
            dbContext.Categories.Add(allJacketsCategory);
            dbContext.Categories.Add(leatherJacketsCategory);
            dbContext.Categories.Add(denimJacketsCategory);
            dbContext.Categories.Add(coatsJacketsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var pantsCategory = new Category { Name = "Pants", Slug = "pants", ParentCategoryId = bottomsCategory.Id };
            var shortsCategory = new Category
                { Name = "Shorts", Slug = "shorts", ParentCategoryId = bottomsCategory.Id };
            var denimCategory = new Category { Name = "Denim", Slug = "denim", ParentCategoryId = bottomsCategory.Id };
            var sweatpantsCategory = new Category
                { Name = "Sweatpants", Slug = "sweatpants", ParentCategoryId = bottomsCategory.Id };
            dbContext.Categories.Add(pantsCategory);
            dbContext.Categories.Add(shortsCategory);
            dbContext.Categories.Add(denimCategory);
            dbContext.Categories.Add(sweatpantsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var suitsCategory = new Category
                { Name = "Suits", Slug = "suits", ParentCategoryId = suitsAndFormalWearCategory.Id };
            var blazersCategory = new Category
                { Name = "Blazers", Slug = "blazers", ParentCategoryId = suitsAndFormalWearCategory.Id };
            var dressShirtsCategory = new Category
                { Name = "Dress Shirts", Slug = "dress-shirts", ParentCategoryId = suitsAndFormalWearCategory.Id };
            var dressPantsCategory = new Category
                { Name = "Dress Pants", Slug = "dress-pants", ParentCategoryId = suitsAndFormalWearCategory.Id };
            dbContext.Categories.Add(suitsCategory);
            dbContext.Categories.Add(blazersCategory);
            dbContext.Categories.Add(dressShirtsCategory);
            dbContext.Categories.Add(dressPantsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var sportsTShirtsCategory = new Category
                { Name = "Sports T-Shirts", Slug = "sports-t-shirts", ParentCategoryId = activeWearCategory.Id };
            var trousersCategory = new Category
                { Name = "Trousers", Slug = "trousers", ParentCategoryId = activeWearCategory.Id };
            var tracksuitsCategory = new Category
                { Name = "Tracksuits", Slug = "tracksuits", ParentCategoryId = activeWearCategory.Id };
            var otherActiveWearCategory = new Category
                { Name = "Other", Slug = "other", ParentCategoryId = activeWearCategory.Id };
            dbContext.Categories.Add(sportsTShirtsCategory);
            dbContext.Categories.Add(trousersCategory);
            dbContext.Categories.Add(tracksuitsCategory);
            dbContext.Categories.Add(otherActiveWearCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var sneakersCategory = new Category
                { Name = "Sneakers", Slug = "sneakers", ParentCategoryId = shoesCat.Id };
            var sandalsCategory = new Category { Name = "Sandals", Slug = "sandals", ParentCategoryId = shoesCat.Id };
            var slippersSlidersCategory = new Category
                { Name = "Slippers & Sliders", Slug = "slippers-sliders", ParentCategoryId = shoesCat.Id };
            var sportShoesCategory = new Category
                { Name = "Sports Shoes", Slug = "sports-shoes", ParentCategoryId = shoesCat.Id };
            var formalShoesCategory = new Category
                { Name = "Formal Shoes", Slug = "formal-shoes", ParentCategoryId = shoesCat.Id };
            var bootsCategory = new Category { Name = "Boots", Slug = "boots", ParentCategoryId = shoesCat.Id };
            dbContext.Categories.Add(sneakersCategory);
            dbContext.Categories.Add(sandalsCategory);
            dbContext.Categories.Add(slippersSlidersCategory);
            dbContext.Categories.Add(sportShoesCategory);
            dbContext.Categories.Add(formalShoesCategory);
            dbContext.Categories.Add(bootsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var footballShoesCategory = new Category
                { Name = "Football Shoes", Slug = "football-shoes", ParentCategoryId = sportShoesCategory.Id };
            var basketballShoesCategory = new Category
                { Name = "Basketball Shoes", Slug = "basketball-shoes", ParentCategoryId = sportShoesCategory.Id };
            var runningShoesCategory = new Category
                { Name = "Running Shoes", Slug = "running-shoes", ParentCategoryId = sportShoesCategory.Id };
            dbContext.Categories.Add(footballShoesCategory);
            dbContext.Categories.Add(basketballShoesCategory);
            dbContext.Categories.Add(runningShoesCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var jewelryCategory = new Category
                { Name = "Jewelry", Slug = "jewelry", ParentCategoryId = accessoriesCat.Id };
            var hatsAndCapsCategory = new Category
                { Name = "Hats and Caps", Slug = "hats-and-caps", ParentCategoryId = accessoriesCat.Id };
            var watchesCategory = new Category
                { Name = "Watches", Slug = "watches", ParentCategoryId = accessoriesCat.Id };
            var bagsCategory = new Category { Name = "Bags", Slug = "bags", ParentCategoryId = accessoriesCat.Id };
            var tiesBeltsCategory = new Category
                { Name = "Ties & Belts", Slug = "ties-belts", ParentCategoryId = accessoriesCat.Id };
            var walletsCategory = new Category
                { Name = "Wallets", Slug = "wallets", ParentCategoryId = accessoriesCat.Id };
            var othersAccessoriesCategory = new Category
                { Name = "Other", Slug = "other", ParentCategoryId = accessoriesCat.Id };
            dbContext.Categories.Add(jewelryCategory);
            dbContext.Categories.Add(hatsAndCapsCategory);
            dbContext.Categories.Add(watchesCategory);
            dbContext.Categories.Add(bagsCategory);
            dbContext.Categories.Add(tiesBeltsCategory);
            dbContext.Categories.Add(walletsCategory);
            dbContext.Categories.Add(othersAccessoriesCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var ringsCategory = new Category { Name = "Rings", Slug = "rings", ParentCategoryId = jewelryCategory.Id };
            var braceletsCategory = new Category
                { Name = "Bracelets", Slug = "bracelets", ParentCategoryId = jewelryCategory.Id };
            dbContext.Categories.Add(ringsCategory);
            dbContext.Categories.Add(braceletsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var backpacksCategory = new Category
                { Name = "Backpacks", Slug = "backpacks", ParentCategoryId = bagsCategory.Id };
            var briefcasesCategory = new Category
                { Name = "Briefcases", Slug = "briefcases", ParentCategoryId = bagsCategory.Id };
            var duffleBagsCategory = new Category
                { Name = "Duffle Bags", Slug = "duffle-bags", ParentCategoryId = bagsCategory.Id };
            dbContext.Categories.Add(backpacksCategory);
            dbContext.Categories.Add(briefcasesCategory);
            dbContext.Categories.Add(duffleBagsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var shavingBeardCareCategory = new Category
                { Name = "Shaving & Beard Care", Slug = "shaving-beard-care", ParentCategoryId = faceBodyCat.Id };
            var skinCareCategory = new Category
                { Name = "Skincare", Slug = "skincare", ParentCategoryId = faceBodyCat.Id };
            var bodyCareCategory = new Category
                { Name = "Body Care", Slug = "body-care", ParentCategoryId = faceBodyCat.Id };
            dbContext.Categories.Add(shavingBeardCareCategory);
            dbContext.Categories.Add(skinCareCategory);
            dbContext.Categories.Add(bodyCareCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var shavingCreamsCategory = new Category
                { Name = "Shaving creams", Slug = "shaving-creams", ParentCategoryId = shavingBeardCareCategory.Id };
            var beardOilsCategory = new Category
                { Name = "Beard Oils", Slug = "beard-oils", ParentCategoryId = shavingBeardCareCategory.Id };
            var aftershavesCategory = new Category
                { Name = "Aftershaves", Slug = "aftershaves", ParentCategoryId = shavingBeardCareCategory.Id };
            dbContext.Categories.Add(shavingCreamsCategory);
            dbContext.Categories.Add(beardOilsCategory);
            dbContext.Categories.Add(aftershavesCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var bathShowerCreamsCategory = new Category
                { Name = "Bath & Shower", Slug = "bath-shower", ParentCategoryId = bodyCareCategory.Id };
            var deodorantsCategory = new Category
                { Name = "Deodorants", Slug = "deodorants", ParentCategoryId = bodyCareCategory.Id };
            var hairCareCategory = new Category
                { Name = "Hair Care", Slug = "hair-care", ParentCategoryId = bodyCareCategory.Id };
            dbContext.Categories.Add(bathShowerCreamsCategory);
            dbContext.Categories.Add(deodorantsCategory);
            dbContext.Categories.Add(hairCareCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var faceWashFiltersCategory = new Category
                { Name = "Face Wash (Filters)", Slug = "face-wash-filters", ParentCategoryId = skinCareCategory.Id };
            var exfoliatorsFiltersCategory = new Category
            {
                Name = "Exfoliators (Filters)", Slug = "exfoliators-filters", ParentCategoryId = skinCareCategory.Id
            };
            var moisturizersFiltersCategory = new Category
            {
                Name = "Moisturizers (Filters)", Slug = "moisturizers-filters", ParentCategoryId = skinCareCategory.Id
            };
            var serumsAndOilsFiltersCategory = new Category
            {
                Name = "Serums and Oils (Filters)", Slug = "serums-and-oils-filters",
                ParentCategoryId = skinCareCategory.Id
            };
            dbContext.Categories.Add(faceWashFiltersCategory);
            dbContext.Categories.Add(exfoliatorsFiltersCategory);
            dbContext.Categories.Add(moisturizersFiltersCategory);
            dbContext.Categories.Add(serumsAndOilsFiltersCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var showerGelsCategory = new Category
                { Name = "Shower Gels", Slug = "shower-gels", ParentCategoryId = bathShowerCreamsCategory.Id };
            var barSoapsCategory = new Category
                { Name = "Bar Soaps", Slug = "bar-soaps", ParentCategoryId = bathShowerCreamsCategory.Id };
            dbContext.Categories.Add(showerGelsCategory);
            dbContext.Categories.Add(barSoapsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var rollOnDeodorantsCategory = new Category
                { Name = "Roll-On Deodorants", Slug = "roll-on-deodorants", ParentCategoryId = deodorantsCategory.Id };
            var sprayDeodorantsCategory = new Category
                { Name = "Spray Deodorants", Slug = "spray-deodorants", ParentCategoryId = deodorantsCategory.Id };
            dbContext.Categories.Add(rollOnDeodorantsCategory);
            dbContext.Categories.Add(sprayDeodorantsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var shampoosConditionersCategory = new Category
            {
                Name = "Shampoos & Conditioners", Slug = "shampoos-conditioners", ParentCategoryId = hairCareCategory.Id
            };
            var creamsAndSerumsCategory = new Category
                { Name = "Creams and Serums", Slug = "creams-and-serums", ParentCategoryId = hairCareCategory.Id };
            var hairMasksCategory = new Category
                { Name = "Hair Masks", Slug = "hair-masks", ParentCategoryId = hairCareCategory.Id };
            dbContext.Categories.Add(shampoosConditionersCategory);
            dbContext.Categories.Add(creamsAndSerumsCategory);
            dbContext.Categories.Add(hairMasksCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var oilsCategory = new Category { Name = "Oils", Slug = "oils", ParentCategoryId = fragrancesCat.Id };
            var colognesCategory = new Category
                { Name = "Colognes", Slug = "colognes", ParentCategoryId = fragrancesCat.Id };
            var eauDeToiletteCategory = new Category
            {
                Name = "Eau De Toilette (Body Sprays*)", Slug = "eau-de-toilette-body-sprays",
                ParentCategoryId = fragrancesCat.Id
            };
            var eauDeParfumCategory = new Category
                { Name = "Eau De Parfum", Slug = "eau-de-parfum", ParentCategoryId = fragrancesCat.Id };
            var giftSetsCategory = new Category
                { Name = "Gift Sets", Slug = "gift-sets", ParentCategoryId = fragrancesCat.Id };
            dbContext.Categories.Add(oilsCategory);
            dbContext.Categories.Add(colognesCategory);
            dbContext.Categories.Add(eauDeToiletteCategory);
            dbContext.Categories.Add(eauDeParfumCategory);
            dbContext.Categories.Add(giftSetsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        private static async Task SeedWomenCategories(IApplicationDbContext dbContext)
        {
            var womenCategory = new Category { Name = "Women", Slug = "women", ParentCategoryId = null };
            dbContext.Categories.Add(womenCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var clothingCategory = new Category { Name = "Clothing", Slug = "clothing", ParentCategoryId = womenCategory.Id };
            var shoesCategory = new Category { Name = "Shoes", Slug = "shoes", ParentCategoryId = womenCategory.Id };
            var accessoriesCategory = new Category { Name = "Accessories", Slug = "accessories", ParentCategoryId = womenCategory.Id };
            var faceBodyCategory = new Category { Name = "Face & Body", Slug = "face-body", ParentCategoryId = womenCategory.Id };
            dbContext.Categories.Add(clothingCategory);            
            dbContext.Categories.Add(shoesCategory);            
            dbContext.Categories.Add(accessoriesCategory);            
            dbContext.Categories.Add(faceBodyCategory);            
            await dbContext.SaveChangesAsync(CancellationToken.None); 
            
            
            var topsCategory = new Category { Name = "Tops", Slug = "tops", ParentCategoryId = clothingCategory.Id };
            dbContext.Categories.Add(topsCategory);    
            await dbContext.SaveChangesAsync(CancellationToken.None); 
            
            var tShirtsCategory = new Category { Name = "T-Shirts", Slug = "t-shirts", ParentCategoryId = topsCategory.Id }; 
            var blousesCategory = new Category { Name = "Blouses", Slug = "blouses", ParentCategoryId = topsCategory.Id };
            var shirtsCategory = new Category { Name = "Shirts", Slug = "shirts", ParentCategoryId = topsCategory.Id };
            var tankTopsCategory = new Category { Name = "Tank Tops", Slug = "tank-tops", ParentCategoryId = topsCategory.Id };
            dbContext.Categories.Add(tShirtsCategory);      
            dbContext.Categories.Add(blousesCategory);      
            dbContext.Categories.Add(shirtsCategory);      
            dbContext.Categories.Add(tankTopsCategory);      
            await dbContext.SaveChangesAsync(CancellationToken.None); 
            
            var sweatersSweatshirtsCategory = new Category { Name = "Sweaters & SweatShirts", Slug = "sweaters-sweatshirts", ParentCategoryId = clothingCategory.Id };
            dbContext.Categories.Add(sweatersSweatshirtsCategory);   
            await dbContext.SaveChangesAsync(CancellationToken.None);
            var sweatersCategory = new Category { Name = "Sweaters", Slug = "sweaters", ParentCategoryId = sweatersSweatshirtsCategory.Id };
            var hoodiesCategory = new Category { Name = "Hoodies", Slug = "hoodies", ParentCategoryId = sweatersSweatshirtsCategory.Id };
            var sweatshirtsCategory = new Category { Name = "SweatShirts", Slug = "sweatshirts", ParentCategoryId = sweatersSweatshirtsCategory.Id };
            var cardigansCategory = new Category { Name = "Cardigans", Slug = "cardigans", ParentCategoryId = sweatersSweatshirtsCategory.Id };
            dbContext.Categories.Add(sweatersCategory);
            dbContext.Categories.Add(hoodiesCategory);
            dbContext.Categories.Add(sweatshirtsCategory);
            dbContext.Categories.Add(cardigansCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None); 
            
            var jacketsCoatsCategory = new Category { Name = "Jackets & Coats", Slug = "jackets-coats", ParentCategoryId = clothingCategory.Id };
            dbContext.Categories.Add(jacketsCoatsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None); 
            var leatherJacketsCategory = new Category { Name = "Leather Jackets (Filter)", Slug = "leather-jackets", ParentCategoryId = jacketsCoatsCategory.Id };
            var denimJacketsCategory = new Category { Name = "Denim Jackets (Filter)", Slug = "denim-jackets", ParentCategoryId = jacketsCoatsCategory.Id };
            var allJacketsCategory = new Category { Name = "All Jackets", Slug = "all-jackets", ParentCategoryId = jacketsCoatsCategory.Id };
            var coatsCategory = new Category { Name = "Coats", Slug = "coats", ParentCategoryId = jacketsCoatsCategory.Id };
            var blazersCategory = new Category { Name = "Blazers", Slug = "blazers", ParentCategoryId = jacketsCoatsCategory.Id };
            dbContext.Categories.Add(leatherJacketsCategory);
            dbContext.Categories.Add(denimJacketsCategory);
            dbContext.Categories.Add(allJacketsCategory);
            dbContext.Categories.Add(coatsCategory);
            dbContext.Categories.Add(blazersCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None); 
            
            var bottomsCategory = new Category { Name = "Bottoms", Slug = "bottoms", ParentCategoryId = clothingCategory.Id };
            dbContext.Categories.Add(bottomsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None); 
            var skirtsCategory = new Category { Name = "Skirts", Slug = "skirts", ParentCategoryId = bottomsCategory.Id };
            var pantsCategory = new Category { Name = "Pants", Slug = "pants", ParentCategoryId = bottomsCategory.Id };
            var shortsCategory = new Category { Name = "Shorts", Slug = "shorts", ParentCategoryId = bottomsCategory.Id };
            var denimsCategory = new Category { Name = "Denim(s)", Slug = "denims", ParentCategoryId = bottomsCategory.Id };
            var sweatpantsCategory = new Category { Name = "Sweatpants", Slug = "sweatpants", ParentCategoryId = bottomsCategory.Id };
            dbContext.Categories.Add(skirtsCategory);
            dbContext.Categories.Add(pantsCategory);
            dbContext.Categories.Add(shortsCategory);
            dbContext.Categories.Add(denimsCategory);
            dbContext.Categories.Add(sweatpantsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None); 
            
            var modestCategory = new Category { Name = "Modest", Slug = "modest", ParentCategoryId = clothingCategory.Id };
            dbContext.Categories.Add(modestCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            var abayasCategory = new Category { Name = "Abayas", Slug = "abayas", ParentCategoryId = modestCategory.Id };
            dbContext.Categories.Add(abayasCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var casualAbayasCategory = new Category { Name = "Casual Abayas (Filter)", Slug = "casual-abayas", ParentCategoryId = abayasCategory.Id };
            var coloredAbayasCategory = new Category { Name = "Colored Abayas (Filter)", Slug = "colored-abayas", ParentCategoryId = abayasCategory.Id };
            var modestActivewearCategory = new Category { Name = "Modest Activewear", Slug = "modest-activewear", ParentCategoryId = modestCategory.Id };
            dbContext.Categories.Add(casualAbayasCategory);
            dbContext.Categories.Add(coloredAbayasCategory);
            dbContext.Categories.Add(modestActivewearCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var dressesCategory = new Category { Name = "Dresses", Slug = "dresses", ParentCategoryId = clothingCategory.Id };
            dbContext.Categories.Add(dressesCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var shortDressesCategory = new Category { Name = "Short Dresses", Slug = "short-dresses", ParentCategoryId = dressesCategory.Id };
            var maxiDressesCategory = new Category { Name = "Maxi Dresses", Slug = "maxi-dresses", ParentCategoryId = dressesCategory.Id };
            dbContext.Categories.Add(shortDressesCategory);
            dbContext.Categories.Add(maxiDressesCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var activewearCategory = new Category { Name = "Activewear", Slug = "activewear", ParentCategoryId = clothingCategory.Id };
            dbContext.Categories.Add(activewearCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var yogaPantsCategory = new Category { Name = "Yoga Pants", Slug = "yoga-pants", ParentCategoryId = activewearCategory.Id };
            var activewearTopsCategory = new Category { Name = "Activewear Tops", Slug = "activewear-tops", ParentCategoryId = activewearCategory.Id };
            var leggingsCategory = new Category { Name = "Leggings", Slug = "leggings", ParentCategoryId = activewearCategory.Id };
            var coOrdsCategory = new Category { Name = "Co-Ords", Slug = "co-ords", ParentCategoryId = activewearCategory.Id };
            var otherClothingCategory = new Category { Name = "Other", Slug = "other", ParentCategoryId = clothingCategory.Id };
            dbContext.Categories.Add(yogaPantsCategory);
            dbContext.Categories.Add(activewearTopsCategory);
            dbContext.Categories.Add(leggingsCategory);
            dbContext.Categories.Add(coOrdsCategory);
            dbContext.Categories.Add(otherClothingCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var sneakersCategory = new Category { Name = "Sneakers", Slug = "sneakers", ParentCategoryId = shoesCategory.Id };
            var sandalsCategory = new Category { Name = "Sandals", Slug = "sandals", ParentCategoryId = shoesCategory.Id };
            var heelsCategory = new Category { Name = "Heels", Slug = "heels", ParentCategoryId = shoesCategory.Id };
            var bootsCategory = new Category { Name = "Boots", Slug = "boots", ParentCategoryId = shoesCategory.Id };
            var sportShoesCategory = new Category { Name = "Sports Shoes", Slug = "sports-shoes", ParentCategoryId = shoesCategory.Id };
            dbContext.Categories.Add(sneakersCategory);
            dbContext.Categories.Add(sandalsCategory);
            dbContext.Categories.Add(heelsCategory);
            dbContext.Categories.Add(bootsCategory);
            dbContext.Categories.Add(sportShoesCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
           
            var jewelryCategory = new Category{Name = "Jewelry", Slug = "jewelry", ParentCategoryId = accessoriesCategory.Id };
            dbContext.Categories.Add(jewelryCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            var necklacesCategory = new Category{Name = "Necklaces", Slug = "necklaces", ParentCategoryId = jewelryCategory.Id };
            var earringsCategory = new Category{Name = "Earrings", Slug = "earrings", ParentCategoryId = jewelryCategory.Id };
            var braceletsCategory = new Category{Name = "Bracelets", Slug = "bracelets", ParentCategoryId = jewelryCategory.Id };
            var ringsCategory = new Category{Name = "Rings", Slug = "rings", ParentCategoryId = jewelryCategory.Id };
            dbContext.Categories.Add(necklacesCategory);
            dbContext.Categories.Add(earringsCategory);
            dbContext.Categories.Add(braceletsCategory);
            dbContext.Categories.Add(ringsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var bodyJewelryCategory = new Category{Name = "Body Jewelry", Slug = "body-jewelry", ParentCategoryId = jewelryCategory.Id };
            var hatsHeadwearCategory = new Category{Name = "Hats & Headwear", Slug = "hats-headwear", ParentCategoryId = accessoriesCategory.Id };
            dbContext.Categories.Add(hatsHeadwearCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            
            var hatsCategory = new Category{Name = "Hats", Slug = "hats", ParentCategoryId = hatsHeadwearCategory.Id };
            var hairAccessoriesCategory = new Category{Name = "Hair Accessories", Slug = "hair-accessories", ParentCategoryId = hatsHeadwearCategory.Id };
            dbContext.Categories.Add(bodyJewelryCategory);
            
            dbContext.Categories.Add(hatsCategory);
            dbContext.Categories.Add(hairAccessoriesCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var scarvesWrapsCategory = new Category{Name = "Scarves & Wraps", Slug = "scarves-wraps", ParentCategoryId = accessoriesCategory.Id };
            dbContext.Categories.Add(scarvesWrapsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var hijabSheilaCategory = new Category{Name = "Hijab (Sheila)", Slug = "hijab-sheila", ParentCategoryId = scarvesWrapsCategory.Id };
            var scarvesCategory = new Category{Name = "Scarves", Slug = "scarves", ParentCategoryId = scarvesWrapsCategory.Id };
            var watchesCategory = new Category{Name = "Watches", Slug = "watches", ParentCategoryId = accessoriesCategory.Id };
            dbContext.Categories.Add(hijabSheilaCategory);
            dbContext.Categories.Add(scarvesCategory);
            dbContext.Categories.Add(watchesCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var bagsPursesCategory = new Category{Name = "Bags & Purses", Slug = "bags-purses", ParentCategoryId = accessoriesCategory.Id };
            dbContext.Categories.Add(bagsPursesCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var handbagsCategory = new Category{Name = "Handbags", Slug = "handbags", ParentCategoryId = bagsPursesCategory.Id };
            var backpacksCategory = new Category{Name = "Backpacks", Slug = "backpacks", ParentCategoryId = bagsPursesCategory.Id };
            var toteBagsCategory = new Category{Name = "Tote Bags", Slug = "tote-bags", ParentCategoryId = bagsPursesCategory.Id };
            var clutchesCategory = new Category{Name = "Clutches", Slug = "clutches", ParentCategoryId = bagsPursesCategory.Id };
            var crossBodyBagsCategory = new Category{Name = "Cross Body Bags", Slug = "cross-body-bags", ParentCategoryId = bagsPursesCategory.Id };
            
            dbContext.Categories.Add(handbagsCategory);
            dbContext.Categories.Add(backpacksCategory);
            dbContext.Categories.Add(toteBagsCategory);
            dbContext.Categories.Add(clutchesCategory);
            dbContext.Categories.Add(crossBodyBagsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var otherAccessoriesCategory = new Category{Name = "Other", Slug = "other", ParentCategoryId = accessoriesCategory.Id };
            var makeupCategory = new Category{Name = "Makeup", Slug = "makeup", ParentCategoryId = faceBodyCategory.Id };
            dbContext.Categories.Add(makeupCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var eyesCategory = new Category{Name = "Eyes", Slug = "eyes", ParentCategoryId = makeupCategory.Id };
            dbContext.Categories.Add(eyesCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var eyeshadowsCategory = new Category{Name = "Eyeshadows", Slug = "eyeshadows", ParentCategoryId = eyesCategory.Id };
            var eyelinerCategory = new Category{Name = "Eyeliner", Slug = "eyeliner", ParentCategoryId = eyesCategory.Id };
            var mascaraCategory = new Category{Name = "Mascara", Slug = "mascara", ParentCategoryId = eyesCategory.Id };
            var eyebrowProductsCategory = new Category{Name = "Eyebrow Products", Slug = "eyebrow-products", ParentCategoryId = eyesCategory.Id };
            dbContext.Categories.Add(otherAccessoriesCategory);
            dbContext.Categories.Add(eyeshadowsCategory);
            dbContext.Categories.Add(eyelinerCategory);
            dbContext.Categories.Add(mascaraCategory);
            dbContext.Categories.Add(eyebrowProductsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var lipsCategory = new Category{Name = "Lips", Slug = "lips", ParentCategoryId = makeupCategory.Id };
            dbContext.Categories.Add(lipsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);  
            
            var lipStickCategory = new Category{Name = "Lip Stick", Slug = "lip-stick", ParentCategoryId = lipsCategory.Id };
            var lipLinerCategory = new Category{Name = "Lip Liner", Slug = "lip-liner", ParentCategoryId = lipsCategory.Id };
            var lipMaskLipBalmsCategory = new Category{Name = "Lip Mask and Lip Balms", Slug = "lip-mask-lip-balms", ParentCategoryId = lipsCategory.Id };
            dbContext.Categories.Add(lipStickCategory);
            dbContext.Categories.Add(lipLinerCategory);
            dbContext.Categories.Add(lipMaskLipBalmsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);            
            
            var toolsAccessoriesCategory = new Category{Name = "Tools & Accessories", Slug = "tools-accessories", ParentCategoryId = makeupCategory.Id };
            dbContext.Categories.Add(toolsAccessoriesCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var makeupBrushesCategory = new Category{Name = "Makeup Brushes", Slug = "makeup-brushes", ParentCategoryId = toolsAccessoriesCategory.Id };
            var makeupSpongesCategory = new Category{Name = "Makeup Sponges", Slug = "makeup-sponges", ParentCategoryId = toolsAccessoriesCategory.Id };
            var mirrorsCategory = new Category{Name = "Mirrors", Slug = "mirrors", ParentCategoryId = toolsAccessoriesCategory.Id };
            var makeupBagsCategory = new Category{Name = "Makeup Bags", Slug = "makeup-bags", ParentCategoryId = toolsAccessoriesCategory.Id };
            dbContext.Categories.Add(makeupBrushesCategory);
            dbContext.Categories.Add(makeupSpongesCategory);
            dbContext.Categories.Add(mirrorsCategory);
            dbContext.Categories.Add(makeupBagsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);            
            
            var faceMakeupCategory = new Category{Name = "Face Makeup", Slug = "face-makeup", ParentCategoryId = makeupCategory.Id };
            dbContext.Categories.Add(faceMakeupCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);        
            
            var primersCategory = new Category{Name = "Primers", Slug = "primers", ParentCategoryId = faceMakeupCategory.Id };
            var foundationsCategory = new Category{Name = "Foundations", Slug = "foundations", ParentCategoryId = faceMakeupCategory.Id };
            var ccBbCreamsCategory = new Category{Name = "(CC and BB Creams) (*)", Slug = "cc-bb-creams", ParentCategoryId = faceMakeupCategory.Id };
            var concealersCategory = new Category{Name = "Concealers", Slug = "concealers", ParentCategoryId = faceMakeupCategory.Id };
            var blushCategory = new Category{Name = "Blush", Slug = "blush", ParentCategoryId = faceMakeupCategory.Id };
            var contourCategory = new Category{Name = "Contour", Slug = "contour", ParentCategoryId = faceMakeupCategory.Id };
            var highlightCategory = new Category{Name = "Highlight", Slug = "highlight", ParentCategoryId = faceMakeupCategory.Id };
            var settingSpraysCategory = new Category{Name = "Setting Sprays", Slug = "setting-sprays", ParentCategoryId = faceMakeupCategory.Id };
            
            dbContext.Categories.Add(primersCategory);
            dbContext.Categories.Add(foundationsCategory);
            dbContext.Categories.Add(ccBbCreamsCategory);
            dbContext.Categories.Add(concealersCategory);
            dbContext.Categories.Add(blushCategory);
            dbContext.Categories.Add(contourCategory);
            dbContext.Categories.Add(highlightCategory);
            dbContext.Categories.Add(settingSpraysCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);              
            
            var skincareCategory = new Category{Name = "Skincare", Slug = "skincare", ParentCategoryId = faceBodyCategory.Id };
            dbContext.Categories.Add(skincareCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);  
            
            var faceWashCategory = new Category{Name = "Face Wash (Filters)", Slug = "face-wash", ParentCategoryId = skincareCategory.Id };
            var exfoliatorsCategory = new Category{Name = "Exfoliators (Filters)", Slug = "exfoliators", ParentCategoryId = skincareCategory.Id };
            var moisturizersCategory = new Category{Name = "Moisturizers (Filters)", Slug = "moisturizers", ParentCategoryId = skincareCategory.Id };
            var serumsOilsCategory = new Category{Name = "Serums and Oils (Filters)", Slug = "serums-oils", ParentCategoryId = skincareCategory.Id };
            dbContext.Categories.Add(faceWashCategory);
            dbContext.Categories.Add(exfoliatorsCategory);
            dbContext.Categories.Add(moisturizersCategory);
            dbContext.Categories.Add(serumsOilsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);              
            
            var bodyCareCategory = new Category{Name = "Body Care", Slug = "body-care", ParentCategoryId = faceBodyCategory.Id };
            dbContext.Categories.Add(bodyCareCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None); 
            
            var bathShowerCategory = new Category{Name = "Bath & Shower", Slug = "bath-shower", ParentCategoryId = bodyCareCategory.Id };
            dbContext.Categories.Add(bathShowerCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);        
            
            var showerGelsCategory = new Category{Name = "Shower Gels", Slug = "shower-gels", ParentCategoryId = bathShowerCategory.Id };
            var barSoapsCategory = new Category{Name = "Bar Soaps", Slug = "bar-soaps", ParentCategoryId = bathShowerCategory.Id };
            var bathBombsCategory = new Category{Name = "Bath Bombs", Slug = "bath-bombs", ParentCategoryId = bathShowerCategory.Id };
            var bodyOilsCategory = new Category{Name = "Body Oils", Slug = "body-oils", ParentCategoryId = bathShowerCategory.Id };
            dbContext.Categories.Add(showerGelsCategory);
            dbContext.Categories.Add(barSoapsCategory);
            dbContext.Categories.Add(bathBombsCategory);
            dbContext.Categories.Add(bodyOilsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);                
            
            var deodorantsCategory = new Category{Name = "Deodorants", Slug = "deodorants", ParentCategoryId = bodyCareCategory.Id };
            dbContext.Categories.Add(deodorantsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);   

            var rollOnDeodorantsCategory = new Category{Name = "Roll-On Deodorants", Slug = "roll-on-deodorants", ParentCategoryId = deodorantsCategory.Id };
            var sprayDeodorantsCategory = new Category{Name = "Spray Deodorants", Slug = "spray-deodorants", ParentCategoryId = deodorantsCategory.Id };
            dbContext.Categories.Add(rollOnDeodorantsCategory);
            dbContext.Categories.Add(sprayDeodorantsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);   
            
            var hairCareCategory = new Category{Name = "Hair Care Products", Slug = "hair-care", ParentCategoryId = faceBodyCategory.Id };
            dbContext.Categories.Add(hairCareCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var shampoosConditionersCategory = new Category{Name = "Shampoos & Conditioners", Slug = "shampoos-conditioners", ParentCategoryId = hairCareCategory.Id };
            var creamsSerumsCategory = new Category{Name = "Creams and Serums", Slug = "creams-serums", ParentCategoryId = hairCareCategory.Id };
            var hairMasksCategory = new Category{Name = "Hair Masks", Slug = "hair-masks", ParentCategoryId = hairCareCategory.Id };
            dbContext.Categories.Add(shampoosConditionersCategory);
            dbContext.Categories.Add(creamsSerumsCategory);
            dbContext.Categories.Add(hairMasksCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            
            var fragrancesCategory = new Category{Name = "Fragrances", Slug = "fragrances", ParentCategoryId = faceBodyCategory.Id };
            dbContext.Categories.Add(fragrancesCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);    
            var oilsCategory = new Category{Name = "Oils", Slug = "oils", ParentCategoryId = fragrancesCategory.Id };
            var colognesCategory = new Category{Name = "Colognes", Slug = "colognes", ParentCategoryId = fragrancesCategory.Id };
            var eauDeToiletteCategory = new Category{Name = "Eau De Toilette (Body Sprays*)", Slug = "eau-de-toilette", ParentCategoryId = fragrancesCategory.Id };
            var eauDeParfumCategory = new Category{Name = "Eau De Parfum", Slug = "eau-de-parfum", ParentCategoryId = fragrancesCategory.Id };
            var giftSetsCategory = new Category{Name = "Gift Sets", Slug = "gift-sets", ParentCategoryId = fragrancesCategory.Id };
            
            dbContext.Categories.Add(oilsCategory);
            dbContext.Categories.Add(colognesCategory);
            dbContext.Categories.Add(eauDeToiletteCategory);
            dbContext.Categories.Add(eauDeParfumCategory);
            dbContext.Categories.Add(giftSetsCategory);
            await dbContext.SaveChangesAsync(CancellationToken.None);            
        }

        private static List<CategoryClosure> CalculateClosureData(List<Category> categories)
        {
            var closureData = new List<CategoryClosure>();

            foreach (var ancestor in categories)
            {
                // Add a closure record for the ancestor to itself (NumLevel = 0)
                closureData.Add(new CategoryClosure
                {
                    AncestorId = ancestor.Id,
                    DescendantId = ancestor.Id,
                    NumLevel = 0
                });

                // Find all descendants of the ancestor
                var descendants = FindDescendants(categories, ancestor);

                // Create closure records for the ancestor and its descendants
                foreach (var descendant in descendants)
                {
                    CreateCategoryClosure(categories, closureData, ancestor, descendant, 1);
                }
            }

            return closureData;
        }

        private static void CreateCategoryClosure(List<Category> categories, List<CategoryClosure> closureData,
            Category ancestor, Category descendant, int numLevel)
        {
            // Check if ancestor and descendant are different
            if (ancestor.Uid != descendant.Uid)
            {
                closureData.Add(new CategoryClosure
                {
                    AncestorId = ancestor.Id,
                    DescendantId = descendant.Id,
                    NumLevel = numLevel
                });

                // Recursively create closure records for ancestor's descendants
                var descendants = FindDescendants(categories, descendant);
                foreach (var subDescendant in descendants)
                {
                    CreateCategoryClosure(categories, closureData, ancestor, subDescendant, numLevel + 1);
                }
            }
        }

        private static List<Category> FindDescendants(List<Category> categories, Category ancestor)
        {
            return categories
                .Where(category => IsDescendant(categories, category, ancestor))
                .ToList();
        }

        private static bool IsDescendant(List<Category> categories, Category category, Category ancestor)
        {
            if (category.Id == ancestor.Id)
            {
                return false; // Ancestor is not considered its own descendant
            }

            // Check if the category's parent Uid matches the ancestor's Uid
            if (category.ParentCategoryId == ancestor.Id)
            {
                return true;
            }

            // Recursively check if the category's parent has the ancestor as its descendant
            if (category.ParentCategoryId != null)
            {
                var parent = categories.SingleOrDefault(c => c.Id == category.ParentCategoryId);
                return IsDescendant(categories, parent, ancestor);
            }

            return false;
        }


        /*private static List<CategoryClosure> CalculateClosureData(List<Category> categories)
        {
            var closureData = new List<CategoryClosure>();

            foreach (var category in categories)
            {
                if (category.ParentCategoryId == null)
                {
                    // Handle root categories
                    CreateCategoryClosure(closureData, category, category, 0);
                }
                else
                {
                    // Find the parent category
                    var parentCategory = categories.FirstOrDefault(c => c.Id == category.ParentCategoryId);
                    if (parentCategory != null)
                    {
                        // Calculate NumLevel based on the parent category's NumLevel
                        var numLevel = closureData.FirstOrDefault(c => c.DescendantId == parentCategory.Id)?.NumLevel +
                            1 ?? 0;
                        CreateCategoryClosure(closureData, parentCategory, category, numLevel);
                    }
                }
            }

            return closureData;
        }

        private static void CreateCategoryClosure(List<CategoryClosure> closureData, Category ancestor, Category descendant, int numLevel)
        {
            closureData.Add(new CategoryClosure
            {
                AncestorId = ancestor.Id,
                DescendantId = descendant.Id,
                NumLevel = numLevel
            });

            if (ancestor.Id != descendant.Id)
            {
                closureData.Add(new CategoryClosure
                {
                    AncestorId = descendant.Id,
                    DescendantId = descendant.Id,
                    NumLevel = 0
                });
            }
        }*/

        /*private async Task CreateCategoryClosure(int ancestorId, int descendantId)
        {
            int numLevel = categoryClosures.Count(c => c.DescendantId == ancestorId);
            categoryClosures.Add(new CategoryClosure
            {
                AncestorId = ancestorId,
                DescendantId = descendantId,
                NumLevel = numLevel
            });
        }*/

        private static async Task SeedIndustries(IApplicationDbContext dbContext)
        {
            try
            {
                // fashion, perfume, accessories, scents, furniture, decor, skincare, makeup
                if (!dbContext.Industries.Any())
                {
                    dbContext.Industries.AddRange(new List<Industry>()
                    {
                        new Industry() { Name = "Fashion", Key = "Fashion" },
                        new Industry() { Name = "Perfume", Key = "Perfume" },
                        new Industry() { Name = "Accessories", Key = "Accessories" },
                        new Industry() { Name = "Scents", Key = "Scents" },
                        new Industry() { Name = "Furniture", Key = "Furniture" },
                        new Industry() { Name = "Decor", Key = "Decor" },
                        new Industry() { Name = "Skincare", Key = "Skincare" },
                        new Industry() { Name = "Makeup", Key = "Makeup" },
                    });
                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static async Task SeedCurrencies(IApplicationDbContext dbContext)
        {
            try
            {
                if (!dbContext.Currencies.Any())
                {
                    dbContext.Currencies.AddRange(new List<Currency>()
                    {
                        new Currency() { Code = "USD", Name = "United States dollar", Symbol = "$" },
                        new Currency() { Code = "GBP", Name = "Sterling", Symbol = "£" },
                        new Currency() { Code = "EUR", Name = "Euro", Symbol = "€" },
                        new Currency() { Code = "AED", Name = "United Arab Emirates dirham", Symbol = "Dh" },
                    });
                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }

                if (!dbContext.GlobalCurrencySettings.Any())
                {
                    var dollar = await dbContext.Currencies.SingleOrDefaultAsync(c => c.Code == "USD");
                    dbContext.GlobalCurrencySettings.Add(new GlobalCurrencySetting()
                    {
                        BaseCurrency = dollar
                    });
                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static async Task SeedRolesAndUsers(UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            try
            {
                foreach (var role in PulrRoles.Roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                var defaultUser = new User
                {
                    UserName = configuration["DefaultUser:Username"],
                    Email = configuration["DefaultUser:Email"],
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    Id = Guid.NewGuid().ToString()
                };

                if (!userManager.Users.Where(u => u.UserName == defaultUser.UserName).Any())
                {
                    var res = await userManager.CreateAsync(defaultUser, configuration["DefaultUser:Password"]);
                    await userManager.AddToRoleAsync(defaultUser,
                        roleManager.Roles.SingleOrDefault(r => r.Name == PulrRoles.Administrator).NormalizedName);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static async Task SeedVibes(IApplicationDbContext dbContext)
        {
            try
            {
                if (!dbContext.Vibes.Any())
                {
                    var vibes = new List<Vibe>
                    {
                        // Fashion & Style
                        new Vibe { Name = "Streetwear", Key = "streetwear", Category = "Fashion & Style", DisplayOrder = 1, IsActive = true },
                        new Vibe { Name = "Trendy", Key = "trendy", Category = "Fashion & Style", DisplayOrder = 2, IsActive = true },
                        new Vibe {Name = "Casual", Key = "casual", Category = "Fashion & Style", DisplayOrder = 3, IsActive = true},
                        new Vibe {Name = "Minimalist", Key = "minimalist", Category = "Fashion & Style", DisplayOrder = 4, IsActive = true},
                        new Vibe {Name = "Luxury", Key = "luxury", Category = "Fashion & Style", DisplayOrder = 5, IsActive = true},
                        new Vibe {Name = "Elegant / Chic", Key = "elegant_chic", Category = "Fashion & Style", DisplayOrder = 6, IsActive = true},
                        new Vibe {Name = "Vintage / Retro", Key = "vintage_retro", Category = "Fashion & Style", DisplayOrder = 7, IsActive = true},
                        new Vibe {Name = "Athleisure", Key = "athleisure", Category = "Fashion & Style", DisplayOrder = 8, IsActive = true},
                        new Vibe {Name = "Cozy / Loungewear", Key = "cozy_loungewear", Category = "Fashion & Style", DisplayOrder = 9, IsActive = true  },

                        // Accessories & Beauty
                        new Vibe {Name = "Jewellery", Key = "jewellery", Category = "Accessories & Beauty", DisplayOrder = 1, IsActive = true},
                        new Vibe {Name = "Bags", Key = "bags", Category = "Accessories & Beauty", DisplayOrder = 2, IsActive = true},
                        new Vibe {Name = "Eyewear", Key = "eyewear", Category = "Accessories & Beauty", DisplayOrder = 3, IsActive = true},
                        new Vibe {Name = "Watches & Timepieces", Key = "watches_timepieces", Category = "Accessories & Beauty", DisplayOrder = 4, IsActive = true},
                        new Vibe {Name = "Statement Accessories", Key = "statement_accessories", Category = "Accessories & Beauty", DisplayOrder = 5, IsActive = true},
                        new Vibe {Name = "Fragrance", Key = "fragrance", Category = "Accessories & Beauty", DisplayOrder = 6, IsActive = true},
                        new Vibe {Name = "Makeup", Key = "makeup", Category = "Accessories & Beauty", DisplayOrder = 7, IsActive = true},
                        new Vibe {Name = "Skincare", Key = "skincare", Category = "Accessories & Beauty", DisplayOrder = 8, IsActive = true},
                        new Vibe {Name = "Nails", Key = "nails", Category = "Accessories & Beauty", DisplayOrder = 9, IsActive = true},
                        new Vibe {Name = "Haircare", Key = "haircare", Category = "Accessories & Beauty", DisplayOrder = 10, IsActive = true},

                        // Home & Wellness
                        new Vibe {Name = "Home & Furniture", Key = "home_furniture", Category = "Home & Wellness", DisplayOrder = 1, IsActive = true},
                        new Vibe {Name = "Décor & Aesthetics", Key = "decor_aesthetics", Category = "Home & Wellness", DisplayOrder = 2, IsActive = true},
                        new Vibe {Name = "Tech & Gadgets", Key = "tech_gadgets", Category = "Home & Wellness", DisplayOrder = 3, IsActive = true},
                        new Vibe {Name = "Wellness & Fitness", Key = "wellness_fitness", Category = "Home & Wellness", DisplayOrder = 4, IsActive = true},
                        new Vibe {Name = "Travel & Vacation", Key = "travel_vacation", Category = "Home & Wellness", DisplayOrder = 5, IsActive = true},

                        // Aesthetic / Subculture Styles
                        new Vibe {Name = "Creative", Key = "creative", Category = "Aesthetic / Subculture Styles", DisplayOrder = 1, IsActive = true},
                        new Vibe {Name = "Alternative", Key = "alternative", Category = "Aesthetic / Subculture Styles", DisplayOrder = 2, IsActive = true},
                        new Vibe {Name = "Natural", Key = "natural", Category = "Aesthetic / Subculture Styles", DisplayOrder = 3, IsActive = true},
                        new Vibe {Name = "Vacation Vibes", Key = "vacation_vibes", Category = "Aesthetic / Subculture Styles", DisplayOrder = 4, IsActive = true},
                        new Vibe {Name = "Traditional", Key = "traditional", Category = "Aesthetic / Subculture Styles", DisplayOrder = 5, IsActive = true},
                        new Vibe {Name = "Pop Culture", Key = "pop_culture", Category = "Aesthetic / Subculture Styles", DisplayOrder = 6, IsActive = true},
                        new Vibe {Name = "Royal", Key = "royal", Category = "Aesthetic / Subculture Styles", DisplayOrder = 7, IsActive = true}
                    };

                    dbContext.Vibes.AddRange(vibes);
                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                throw new Exception("An error occurred while seeding vibes", e);
            }
        }
    }
}