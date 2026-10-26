using System.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>트럭 장사 데이터 (손님과 장사 한 번 손님 수)</summary>
    public class TruckDataTests
    {
        [Fact]
        public void 손님은_열두_명이고_주문에_요리_자리가_있고_그림이_있다()
        {
            var cs = TestData.Full.Dialogue.Customers;
            Assert.Equal(12, cs.Count);
            string dir = System.IO.Path.Combine(System.AppContext.BaseDirectory, "../../../../../godot/art/customers");
            foreach (var c in cs)
                foreach (var pose in new[] { "stand", "walk", "happy", "sad" })
                    Assert.True(System.IO.File.Exists(System.IO.Path.Combine(dir, c.Id + "-" + pose + ".png")), c.Id + "-" + pose + ".png 이 없다");
            Assert.Equal(cs.Count, cs.Select(c => c.Id).Distinct().Count());
            Assert.All(cs, c => Assert.Contains("{0}", c.Order));
        }

        [Fact]
        public void 장사_한_번에_손님_다섯_명()
        {
            Assert.Equal(5, TestData.Game.Balance.Truck.CustomersPerShift);
        }
    }
}
