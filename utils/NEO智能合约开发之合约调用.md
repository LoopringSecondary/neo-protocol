# NEO智能合约开发:合约调用

小蚁智能合约可以在合约中继续调用其他合约，这在功能和灵活性上都非常有帮助。总的来说，NEO智能合约中的合约调用有两种模式：静态调用和动态调用。  
*  静态调用：被调用合约Hash写死在合约中，编译时即已确定。
*  动态调用：被调用合约Hash作为参数在合约调用的时候传入，即运行时才能确定。
## 两种模式的对比
| 类别  | 静态调用 |  动态调用 |
|----------|:-------------:|----------:|
| 加载时刻 |    编译时   |   运行时  |
| 费用 |  使用了静态调用的合约部署需要490gas | 动态的更加昂贵，990gas |
| 可扩展性 |    低   |   高  |

## 写法
### 静态调用
```
[Appcall("06fa8be9b6609d963e8fc63977b9f8dc5f10895f")]
static extern object CallLrn(string method, object[] arr);

public static void Main()
{
    byte[] from = "AZH65BFNKUGcUbFTH5ZYD89GxgGFm1j9Ht".ToScriptHash();
    byte[] to = "AcvectucamfYVaZmy6oqaN4G5k7ABG7S4w".ToScriptHash();
    BigInteger withdrawAmount = 1000;
    // call lrn transfer
    byte[] result = (byte[])CallLrn("transfer", new object[] { from, to, withdrawAmount });
}
```

### 动态调用
```
public delegate object NEP5Contract(string method, object[] args);
public static void Main(byte[] assetId)
{
    byte[] owner = "AZH65BFNKUGcUbFTH5ZYD89GxgGFm1j9Ht".ToScriptHash();
    var balanceArgs = new object[] { owner };
    var balanceContract = (NEP5Contract)assetId.ToDelegate();
    BigInteger balanceResult = (BigInteger)balanceContract("balanceOf", balanceArgs);
}
```
以上两种调用方法都是实际项目里验证过的，有一个要注意的点是：使用中静态调用和动态调用合约地址的大小端是不一样的，比如LRN token scripHash 大端是：0x06fa8be9b6609d963e8fc63977b9f8dc5f10895f, 那么静态调用代码里写的就是06fa8be9b6609d963e8fc63977b9f8dc5f10895f，但是动态调用的时候实际出入的却需要是小端的：5f89105fdcf8b97739c68f3e969d60b6e98bfa06，这是使用上的一个坑。
