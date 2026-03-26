using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence
{
    public static class ProductCategoriesSeed
    {
        public static async Task SeedProductCategories(IApplicationDbContext dbContext)
        {
            // Check if product categories already exist
            if (dbContext.SubCategoryLevel1s.Any(s => s.Slug == "product-clothing"))
            {
                return; // Already seeded
            }

            // Create or get the parent "Products" category
            var productsCategory = await dbContext.Categories
                .FirstOrDefaultAsync(c => c.Slug == "products");
            
            if (productsCategory == null)
            {
                productsCategory = new Category { Name = "Products", Slug = "products", ParentCategoryId = null };
                dbContext.Categories.Add(productsCategory);
                await dbContext.SaveChangesAsync(CancellationToken.None);
            }

            // 1. Clothing
            var clothingCat = new SubCategoryLevel1 { Name = "Clothing", Slug = "product-clothing", CategoryId = productsCategory.Id };
            dbContext.SubCategoryLevel1s.Add(clothingCat);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var clothingSubcategories = new List<SubCategoryLevel2>
            {
                new SubCategoryLevel2 { Name = "Tops", Slug = "tops", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Bottoms", Slug = "bottoms", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Dresses", Slug = "dresses", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Jumpsuits", Slug = "jumpsuits", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Jackets", Slug = "jackets", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Coats", Slug = "coats", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Blazers", Slug = "blazers", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Hoodies", Slug = "hoodies", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Activewear", Slug = "activewear", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Athleisure", Slug = "athleisure", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Loungewear", Slug = "loungewear", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Sleepwear", Slug = "sleepwear", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Swimwear", Slug = "swimwear", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Traditional", Slug = "traditional", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Winterwear", Slug = "winterwear", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Rainwear", Slug = "rainwear", SubCategoryLevel1Id = clothingCat.Id },
                new SubCategoryLevel2 { Name = "Beachwear", Slug = "beachwear", SubCategoryLevel1Id = clothingCat.Id }
            };
            dbContext.SubCategoryLevel2s.AddRange(clothingSubcategories);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            // 2. Footwear
            var footwearCat = new SubCategoryLevel1 { Name = "Footwear", Slug = "product-footwear", CategoryId = productsCategory.Id };
            dbContext.SubCategoryLevel1s.Add(footwearCat);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var footwearSubcategories = new List<SubCategoryLevel2>
            {
                new SubCategoryLevel2 { Name = "Sneakers", Slug = "sneakers", SubCategoryLevel1Id = footwearCat.Id },
                new SubCategoryLevel2 { Name = "Heels", Slug = "heels", SubCategoryLevel1Id = footwearCat.Id },
                new SubCategoryLevel2 { Name = "Flats", Slug = "flats", SubCategoryLevel1Id = footwearCat.Id },
                new SubCategoryLevel2 { Name = "Boots", Slug = "boots", SubCategoryLevel1Id = footwearCat.Id },
                new SubCategoryLevel2 { Name = "Sandals", Slug = "sandals", SubCategoryLevel1Id = footwearCat.Id },
                new SubCategoryLevel2 { Name = "Slippers", Slug = "slippers", SubCategoryLevel1Id = footwearCat.Id },
                new SubCategoryLevel2 { Name = "Loafers", Slug = "loafers", SubCategoryLevel1Id = footwearCat.Id },
                new SubCategoryLevel2 { Name = "Sports Shoes", Slug = "sports-shoes", SubCategoryLevel1Id = footwearCat.Id }
            };
            dbContext.SubCategoryLevel2s.AddRange(footwearSubcategories);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            // 3. Bags & Accessories
            var bagsAccessoriesCat = new SubCategoryLevel1 { Name = "Bags & Accessories", Slug = "product-bags-accessories", CategoryId = productsCategory.Id };
            dbContext.SubCategoryLevel1s.Add(bagsAccessoriesCat);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var bagsAccessoriesSubcategories = new List<SubCategoryLevel2>
            {
                new SubCategoryLevel2 { Name = "Handbags", Slug = "handbags", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Backpacks", Slug = "backpacks", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Totes", Slug = "totes", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Wallets", Slug = "wallets", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Cardholders", Slug = "cardholders", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Belts", Slug = "belts", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Hats", Slug = "hats", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Caps", Slug = "caps", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Scarves", Slug = "scarves", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Shawls", Slug = "shawls", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Sunglasses", Slug = "sunglasses", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Eyewear", Slug = "eyewear", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Gloves", Slug = "gloves", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Socks", Slug = "socks", SubCategoryLevel1Id = bagsAccessoriesCat.Id },
                new SubCategoryLevel2 { Name = "Hosiery", Slug = "hosiery", SubCategoryLevel1Id = bagsAccessoriesCat.Id }
            };
            dbContext.SubCategoryLevel2s.AddRange(bagsAccessoriesSubcategories);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            // 4. Jewelry & Watches
            var jewelryWatchesCat = new SubCategoryLevel1 { Name = "Jewelry & Watches", Slug = "product-jewelry-watches", CategoryId = productsCategory.Id };
            dbContext.SubCategoryLevel1s.Add(jewelryWatchesCat);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var jewelryWatchesSubcategories = new List<SubCategoryLevel2>
            {
                new SubCategoryLevel2 { Name = "Necklaces", Slug = "necklaces", SubCategoryLevel1Id = jewelryWatchesCat.Id },
                new SubCategoryLevel2 { Name = "Earrings", Slug = "earrings", SubCategoryLevel1Id = jewelryWatchesCat.Id },
                new SubCategoryLevel2 { Name = "Bracelets", Slug = "bracelets", SubCategoryLevel1Id = jewelryWatchesCat.Id },
                new SubCategoryLevel2 { Name = "Rings", Slug = "rings", SubCategoryLevel1Id = jewelryWatchesCat.Id },
                new SubCategoryLevel2 { Name = "Watches", Slug = "watches", SubCategoryLevel1Id = jewelryWatchesCat.Id },
                new SubCategoryLevel2 { Name = "Anklets", Slug = "anklets", SubCategoryLevel1Id = jewelryWatchesCat.Id },
                new SubCategoryLevel2 { Name = "Brooches", Slug = "brooches", SubCategoryLevel1Id = jewelryWatchesCat.Id },
                new SubCategoryLevel2 { Name = "Pins", Slug = "pins", SubCategoryLevel1Id = jewelryWatchesCat.Id }
            };
            dbContext.SubCategoryLevel2s.AddRange(jewelryWatchesSubcategories);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            // 5. Beauty & Skincare
            var beautySkincarecat = new SubCategoryLevel1 { Name = "Beauty & Skincare", Slug = "product-beauty-skincare", CategoryId = productsCategory.Id };
            dbContext.SubCategoryLevel1s.Add(beautySkincarecat);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var beautySkincareSubcategories = new List<SubCategoryLevel2>
            {
                new SubCategoryLevel2 { Name = "Cleansers", Slug = "cleansers", SubCategoryLevel1Id = beautySkincarecat.Id },
                new SubCategoryLevel2 { Name = "Toners", Slug = "toners", SubCategoryLevel1Id = beautySkincarecat.Id },
                new SubCategoryLevel2 { Name = "Serums", Slug = "serums", SubCategoryLevel1Id = beautySkincarecat.Id },
                new SubCategoryLevel2 { Name = "Moisturizers", Slug = "moisturizers", SubCategoryLevel1Id = beautySkincarecat.Id },
                new SubCategoryLevel2 { Name = "Sunscreens", Slug = "sunscreens", SubCategoryLevel1Id = beautySkincarecat.Id },
                new SubCategoryLevel2 { Name = "Masks", Slug = "masks", SubCategoryLevel1Id = beautySkincarecat.Id },
                new SubCategoryLevel2 { Name = "Treatments", Slug = "treatments", SubCategoryLevel1Id = beautySkincarecat.Id },
                new SubCategoryLevel2 { Name = "Makeup Removers", Slug = "makeup-removers", SubCategoryLevel1Id = beautySkincarecat.Id }
            };
            dbContext.SubCategoryLevel2s.AddRange(beautySkincareSubcategories);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            // 6. Haircare
            var haircareCat = new SubCategoryLevel1 { Name = "Haircare", Slug = "product-haircare", CategoryId = productsCategory.Id };
            dbContext.SubCategoryLevel1s.Add(haircareCat);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var haircareSubcategories = new List<SubCategoryLevel2>
            {
                new SubCategoryLevel2 { Name = "Shampoo", Slug = "shampoo", SubCategoryLevel1Id = haircareCat.Id },
                new SubCategoryLevel2 { Name = "Conditioner", Slug = "conditioner", SubCategoryLevel1Id = haircareCat.Id },
                new SubCategoryLevel2 { Name = "Hair Oils", Slug = "hair-oils", SubCategoryLevel1Id = haircareCat.Id },
                new SubCategoryLevel2 { Name = "Serums", Slug = "hair-serums", SubCategoryLevel1Id = haircareCat.Id },
                new SubCategoryLevel2 { Name = "Hair Masks", Slug = "hair-masks", SubCategoryLevel1Id = haircareCat.Id },
                new SubCategoryLevel2 { Name = "Styling Products", Slug = "styling-products", SubCategoryLevel1Id = haircareCat.Id },
                new SubCategoryLevel2 { Name = "Tools & Accessories", Slug = "hair-tools-accessories", SubCategoryLevel1Id = haircareCat.Id },
                new SubCategoryLevel2 { Name = "Brushes", Slug = "brushes", SubCategoryLevel1Id = haircareCat.Id },
                new SubCategoryLevel2 { Name = "Straighteners", Slug = "straighteners", SubCategoryLevel1Id = haircareCat.Id }
            };
            dbContext.SubCategoryLevel2s.AddRange(haircareSubcategories);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            // 7. Bodycare
            var bodycareCat = new SubCategoryLevel1 { Name = "Bodycare", Slug = "product-bodycare", CategoryId = productsCategory.Id };
            dbContext.SubCategoryLevel1s.Add(bodycareCat);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var bodycareSubcategories = new List<SubCategoryLevel2>
            {
                new SubCategoryLevel2 { Name = "Body Wash", Slug = "body-wash", SubCategoryLevel1Id = bodycareCat.Id },
                new SubCategoryLevel2 { Name = "Lotions & Oils", Slug = "lotions-oils", SubCategoryLevel1Id = bodycareCat.Id },
                new SubCategoryLevel2 { Name = "Deodorants", Slug = "deodorants", SubCategoryLevel1Id = bodycareCat.Id },
                new SubCategoryLevel2 { Name = "Hand & Foot Care", Slug = "hand-foot-care", SubCategoryLevel1Id = bodycareCat.Id },
                new SubCategoryLevel2 { Name = "Bath Essentials", Slug = "bath-essentials", SubCategoryLevel1Id = bodycareCat.Id },
                new SubCategoryLevel2 { Name = "Scrubs", Slug = "scrubs", SubCategoryLevel1Id = bodycareCat.Id },
                new SubCategoryLevel2 { Name = "Salts", Slug = "salts", SubCategoryLevel1Id = bodycareCat.Id }
            };
            dbContext.SubCategoryLevel2s.AddRange(bodycareSubcategories);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            // 8. Fragrance
            var fragranceCat = new SubCategoryLevel1 { Name = "Fragrance", Slug = "product-fragrance", CategoryId = productsCategory.Id };
            dbContext.SubCategoryLevel1s.Add(fragranceCat);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var fragranceSubcategories = new List<SubCategoryLevel2>
            {
                new SubCategoryLevel2 { Name = "Perfumes", Slug = "perfumes", SubCategoryLevel1Id = fragranceCat.Id },
                new SubCategoryLevel2 { Name = "Body Mists", Slug = "body-mists", SubCategoryLevel1Id = fragranceCat.Id },
                new SubCategoryLevel2 { Name = "Essential Oils", Slug = "essential-oils", SubCategoryLevel1Id = fragranceCat.Id },
                new SubCategoryLevel2 { Name = "Roll-ons", Slug = "roll-ons", SubCategoryLevel1Id = fragranceCat.Id },
                new SubCategoryLevel2 { Name = "Home Fragrance", Slug = "home-fragrance", SubCategoryLevel1Id = fragranceCat.Id },
                new SubCategoryLevel2 { Name = "Candles", Slug = "candles", SubCategoryLevel1Id = fragranceCat.Id },
                new SubCategoryLevel2 { Name = "Diffusers", Slug = "diffusers", SubCategoryLevel1Id = fragranceCat.Id }
            };
            dbContext.SubCategoryLevel2s.AddRange(fragranceSubcategories);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            // 9. Health & Wellness
            var healthWellnessCat = new SubCategoryLevel1 { Name = "Health & Wellness", Slug = "product-health-wellness", CategoryId = productsCategory.Id };
            dbContext.SubCategoryLevel1s.Add(healthWellnessCat);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var healthWellnessSubcategories = new List<SubCategoryLevel2>
            {
                new SubCategoryLevel2 { Name = "Supplements & Vitamins", Slug = "supplements-vitamins", SubCategoryLevel1Id = healthWellnessCat.Id },
                new SubCategoryLevel2 { Name = "Herbal", Slug = "herbal", SubCategoryLevel1Id = healthWellnessCat.Id },
                new SubCategoryLevel2 { Name = "Organic Products", Slug = "organic-products", SubCategoryLevel1Id = healthWellnessCat.Id },
                new SubCategoryLevel2 { Name = "Aromatherapy", Slug = "aromatherapy", SubCategoryLevel1Id = healthWellnessCat.Id },
                new SubCategoryLevel2 { Name = "Sleep & Relaxation Aids", Slug = "sleep-relaxation-aids", SubCategoryLevel1Id = healthWellnessCat.Id },
                new SubCategoryLevel2 { Name = "Mindfulness", Slug = "mindfulness", SubCategoryLevel1Id = healthWellnessCat.Id },
                new SubCategoryLevel2 { Name = "Meditation Tools", Slug = "meditation-tools", SubCategoryLevel1Id = healthWellnessCat.Id }
            };
            dbContext.SubCategoryLevel2s.AddRange(healthWellnessSubcategories);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            // 10. Fitness & Activewear
            var fitnessActivewearCat = new SubCategoryLevel1 { Name = "Fitness & Activewear", Slug = "product-fitness-activewear", CategoryId = productsCategory.Id };
            dbContext.SubCategoryLevel1s.Add(fitnessActivewearCat);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var fitnessActivewearSubcategories = new List<SubCategoryLevel2>
            {
                new SubCategoryLevel2 { Name = "Sportswear", Slug = "sportswear", SubCategoryLevel1Id = fitnessActivewearCat.Id },
                new SubCategoryLevel2 { Name = "Yoga Wear", Slug = "yoga-wear", SubCategoryLevel1Id = fitnessActivewearCat.Id },
                new SubCategoryLevel2 { Name = "Gym Accessories", Slug = "gym-accessories", SubCategoryLevel1Id = fitnessActivewearCat.Id },
                new SubCategoryLevel2 { Name = "Bottles", Slug = "bottles", SubCategoryLevel1Id = fitnessActivewearCat.Id },
                new SubCategoryLevel2 { Name = "Towels", Slug = "towels", SubCategoryLevel1Id = fitnessActivewearCat.Id },
                new SubCategoryLevel2 { Name = "Mats", Slug = "mats", SubCategoryLevel1Id = fitnessActivewearCat.Id },
                new SubCategoryLevel2 { Name = "Resistance Bands", Slug = "resistance-bands", SubCategoryLevel1Id = fitnessActivewearCat.Id },
                new SubCategoryLevel2 { Name = "Equipment", Slug = "equipment", SubCategoryLevel1Id = fitnessActivewearCat.Id }
            };
            dbContext.SubCategoryLevel2s.AddRange(fitnessActivewearSubcategories);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            // 11. Home & Lifestyle
            var homeLifestyleCat = new SubCategoryLevel1 { Name = "Home & Lifestyle", Slug = "product-home-lifestyle", CategoryId = productsCategory.Id };
            dbContext.SubCategoryLevel1s.Add(homeLifestyleCat);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var homeLifestyleSubcategories = new List<SubCategoryLevel2>
            {
                new SubCategoryLevel2 { Name = "Home Decor", Slug = "home-decor", SubCategoryLevel1Id = homeLifestyleCat.Id },
                new SubCategoryLevel2 { Name = "Candles", Slug = "home-candles", SubCategoryLevel1Id = homeLifestyleCat.Id },
                new SubCategoryLevel2 { Name = "Art", Slug = "art", SubCategoryLevel1Id = homeLifestyleCat.Id },
                new SubCategoryLevel2 { Name = "Vases", Slug = "vases", SubCategoryLevel1Id = homeLifestyleCat.Id },
                new SubCategoryLevel2 { Name = "Plants", Slug = "plants", SubCategoryLevel1Id = homeLifestyleCat.Id },
                new SubCategoryLevel2 { Name = "Home Fragrance", Slug = "home-fragrance-lifestyle", SubCategoryLevel1Id = homeLifestyleCat.Id },
                new SubCategoryLevel2 { Name = "Diffusers", Slug = "home-diffusers", SubCategoryLevel1Id = homeLifestyleCat.Id },
                new SubCategoryLevel2 { Name = "Room Sprays", Slug = "room-sprays", SubCategoryLevel1Id = homeLifestyleCat.Id },
                new SubCategoryLevel2 { Name = "Stationery & Journals", Slug = "stationery-journals", SubCategoryLevel1Id = homeLifestyleCat.Id },
                new SubCategoryLevel2 { Name = "Pet Accessories", Slug = "pet-accessories", SubCategoryLevel1Id = homeLifestyleCat.Id },
                new SubCategoryLevel2 { Name = "Art & Collectibles", Slug = "art-collectibles", SubCategoryLevel1Id = homeLifestyleCat.Id }
            };
            dbContext.SubCategoryLevel2s.AddRange(homeLifestyleSubcategories);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }
}
