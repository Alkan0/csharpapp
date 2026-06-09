using CSharpApp.Core.Dtos;
using CSharpApp.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;

namespace CSharpApp.Api.Tests;

public sealed class ApiTestApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProductsService>();
            services.RemoveAll<ICategoriesService>();
            services.RemoveAll<IAuthService>();

            services.AddSingleton<IProductsService, FakeProductsService>();
            services.AddSingleton<ICategoriesService, FakeCategoriesService>();
            services.AddSingleton<IAuthService, FakeAuthService>();
        });
    }

    private sealed class FakeProductsService : IProductsService
    {
        public Task<IReadOnlyCollection<Product>> GetProducts()
        {
            IReadOnlyCollection<Product> products =
            [
                CreateProduct(1, "Test product")
            ];

            return Task.FromResult(products);
        }

        public Task<Product?> GetProduct(int id)
        {
            if (id == 503)
            {
                throw new HttpRequestException("Third-party products service failed.", null, HttpStatusCode.ServiceUnavailable);
            }

            return Task.FromResult(id == 1 ? CreateProduct(1, "Test product") : null);
        }

        public Task<Product?> CreateProduct(CreateProductRequest request)
        {
            if (request.Title == "invalid")
            {
                throw new HttpRequestException("Invalid product payload", null, HttpStatusCode.BadRequest);
            }

            return Task.FromResult<Product?>(CreateProduct(100, request.Title ?? "Created product"));
        }

        private static Product CreateProduct(int id, string title)
        {
            return new Product
            {
                Id = id,
                Title = title,
                Price = 10,
                Description = "Test description",
                Images = { "https://example.com/product.png" },
                Category = new Category
                {
                    Id = 1,
                    Name = "Test category",
                    Image = "https://example.com/category.png"
                }
            };
        }
    }

    private sealed class FakeCategoriesService : ICategoriesService
    {
        public Task<IReadOnlyCollection<Category>> GetCategories()
        {
            IReadOnlyCollection<Category> categories =
            [
                CreateCategory(1, "Test category")
            ];

            return Task.FromResult(categories);
        }

        public Task<Category?> GetCategory(int id)
        {
            return Task.FromResult(id == 1 ? CreateCategory(1, "Test category") : null);
        }

        public Task<Category?> CreateCategory(CreateCategoryRequest request)
        {
            if (request.Name == "invalid")
            {
                throw new HttpRequestException("Invalid category payload", null, HttpStatusCode.BadRequest);
            }

            return Task.FromResult<Category?>(CreateCategory(100, request.Name ?? "Created category"));
        }

        public Task<Category?> UpdateCategory(int id, UpdateCategoryRequest request)
        {
            return Task.FromResult(id == 1 ? CreateCategory(1, request.Name ?? "Updated category") : null);
        }

        public Task<bool> DeleteCategory(int id)
        {
            return Task.FromResult(id == 1);
        }

        public Task<IReadOnlyCollection<Product>> GetCategoryProducts(int id)
        {
            IReadOnlyCollection<Product> products =
            [
                new Product
                {
                    Id = 1,
                    Title = "Category product",
                    Category = CreateCategory(id, "Test category")
                }
            ];

            return Task.FromResult(products);
        }

        private static Category CreateCategory(int id, string name)
        {
            return new Category
            {
                Id = id,
                Name = name,
                Image = "https://example.com/category.png"
            };
        }
    }

    private sealed class FakeAuthService : IAuthService
    {
        public Task<AuthTokenResponse?> Login(LoginRequest request)
        {
            if (request.Email == "john@mail.com" && request.Password == "changeme")
            {
                return Task.FromResult<AuthTokenResponse?>(new AuthTokenResponse
                {
                    AccessToken = "test-access-token",
                    RefreshToken = "test-refresh-token"
                });
            }

            return Task.FromResult<AuthTokenResponse?>(null);
        }

        public Task<AuthTokenResponse?> RefreshToken(RefreshTokenRequest request)
        {
            if (request.RefreshToken == "test-refresh-token")
            {
                return Task.FromResult<AuthTokenResponse?>(new AuthTokenResponse
                {
                    AccessToken = "refreshed-access-token",
                    RefreshToken = "refreshed-refresh-token"
                });
            }

            return Task.FromResult<AuthTokenResponse?>(null);
        }

        public Task<AuthProfileResponse?> GetProfile(string accessToken)
        {
            if (accessToken != "test-access-token")
            {
                return Task.FromResult<AuthProfileResponse?>(null);
            }

            return Task.FromResult<AuthProfileResponse?>(new AuthProfileResponse
            {
                Id = 1,
                Email = "john@mail.com",
                Name = "John Doe",
                Role = "customer",
                Avatar = "https://example.com/avatar.png"
            });
        }
    }
}
