using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using AdvertisementServiceMVC2.Controllers;
using AdvertisementServiceMVC2.Models;
using Microsoft.EntityFrameworkCore.Query;

namespace AdvertisementServiceMVC2.Tests
{
    public class AllControllersTests
    {
        private readonly Mock<AdvertisementServiceContext> _mockContext;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;

        public AllControllersTests()
        {
            _mockContext = new Mock<AdvertisementServiceContext>(new DbContextOptions<AdvertisementServiceContext>());

            // Настройка UserManager
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(
                userStoreMock.Object,
                It.IsAny<IOptions<IdentityOptions>>(),
                It.IsAny<IPasswordHasher<AppUser>>(),
                new List<IUserValidator<AppUser>>(),
                new List<IPasswordValidator<AppUser>>(),
                new Mock<ILookupNormalizer>().Object,
                new IdentityErrorDescriber(),
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<AppUser>>>().Object
            );

            _mockEnvironment = new Mock<IWebHostEnvironment>();
        }

        // ==========================================
        // HELPERS
        // ==========================================

        // Метод для создания Mock DbSet, который поддерживает Async операции
        private static Mock<DbSet<T>> CreateDbSetMock<T>(IEnumerable<T> elements) where T : class
        {
            var elementsAsQueryable = elements.AsQueryable();
            var dbSetMock = new Mock<DbSet<T>>();

            dbSetMock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(elementsAsQueryable.GetEnumerator()));

            dbSetMock.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(elementsAsQueryable.Provider));

            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(elementsAsQueryable.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(elementsAsQueryable.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(elementsAsQueryable.GetEnumerator());

            return dbSetMock;
        }

        // ==========================================
        // TESTS FOR AdvertisementsController
        // ==========================================

        [Fact]
        public async Task AdvertisementsController_Index_ReturnsViewWithData()
        {
            // Arrange
            var ads = new List<Advertisement>
            {
                new Advertisement { Id = 1, Title = "Test Ad 1", Status = "Active", CreatedAt = DateTime.Now },
                new Advertisement { Id = 2, Title = "Test Ad 2", Status = "Active", CreatedAt = DateTime.Now }
            };

            var mockSet = CreateDbSetMock(ads);

            // Мокаем и Advertisements, и Categories, и Regions, так как контроллер к ним обращается
            var categories = new List<Category>();
            var regions = new List<Region>();

            _mockContext.Setup(c => c.Advertisements).Returns(mockSet.Object);
            _mockContext.Setup(c => c.Categories).Returns(CreateDbSetMock(categories).Object);
            _mockContext.Setup(c => c.Regions).Returns(CreateDbSetMock(regions).Object);

            var controller = new AdvertisementsController(
                _mockContext.Object,
                _mockUserManager.Object,
                _mockEnvironment.Object
            );

            // Act
            // ИСПРАВЛЕНО: Передаем пустую модель фильтра, чтобы избежать NullReferenceException
            var filter = new FilterViewModel();
            var result = await controller.Index(filter);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            // Проверяем, что вернулась модель. Либо список объявлений, либо сама ViewModel, 
            // зависит от реализации контроллера. Обычно это FilterViewModel.
            Assert.NotNull(viewResult.Model);
        }

        [Fact]
        public async Task AdvertisementsController_Details_ReturnsViewForExistingAd()
        {
            // Arrange
            var ad = new Advertisement { Id = 1, Title = "Test Ad" };
            var ads = new List<Advertisement> { ad };
            var mockSet = CreateDbSetMock(ads);

            _mockContext.Setup(c => c.Advertisements).Returns(mockSet.Object);

            var controller = new AdvertisementsController(
                _mockContext.Object,
                _mockUserManager.Object,
                _mockEnvironment.Object
            );

            // Act
            var result = await controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }

        // ==========================================
        // TESTS FOR CategoriesController
        // ==========================================

        [Fact]
        public async Task CategoriesController_Index_ReturnsViewWithCategories()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { CategoryID = 1, CategoryName = "Category 1" },
                new Category { CategoryID = 2, CategoryName = "Category 2" }
            };

            var mockSet = CreateDbSetMock(categories);
            _mockContext.Setup(c => c.Categories).Returns(mockSet.Object);

            var controller = new CategoriesController(_mockContext.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Category>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task CategoriesController_Details_ReturnsViewForExistingCategory()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Test Category" };
            var categories = new List<Category> { category };
            var mockSet = CreateDbSetMock(categories);

            _mockContext.Setup(c => c.Categories).Returns(mockSet.Object);

            var controller = new CategoriesController(_mockContext.Object);

            // Act
            var result = await controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
        }

        // ==========================================
        // TESTS FOR UsersController
        // ==========================================

        [Fact]
        public async Task UsersController_Index_ReturnsViewWithUsers()
        {
            // Arrange
            var users = new List<AppUser>
            {
                new AppUser { Id = "1", Name = "User 1" },
                new AppUser { Id = "2", Name = "User 2" }
            };

            var mockSet = CreateDbSetMock(users);
            _mockContext.Setup(c => c.Users).Returns(mockSet.Object);

            var controller = new UsersController(_mockContext.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<AppUser>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        // ==========================================
        // TESTS FOR HomeController
        // ==========================================

        [Fact]
        public void HomeController_Index_ReturnsView()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(mockLogger.Object);

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void HomeController_Error_ReturnsView()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(mockLogger.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
        }
    }

    // ========================================================================
    // ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ ДЛЯ ASYNC QUERY PROVIDER
    // ========================================================================

    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(
                    name: nameof(IQueryProvider.Execute),
                    genericParameterCount: 1,
                    types: new[] { typeof(Expression) })
                .MakeGenericMethod(resultType)
                .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                ?.MakeGenericMethod(resultType)
                .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider
        {
            get { return new TestAsyncQueryProvider<T>(this); }
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }

        public T Current
        {
            get { return _inner.Current; }
        }
    }
}