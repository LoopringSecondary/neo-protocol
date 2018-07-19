# NEO智能合约开发之空投NEP5资产
## 先介绍两篇相关资料
* [NEO锁仓](http://docs.neo.org/zh-cn/sc/tutorial/Lock.html)
* [UTXO模型资产如何通过智能合约空投](https://mp.weixin.qq.com/s/qe7lV71__FygKxO9E4_Oew) 
  
## 下面介绍下NEP5类资产如何通过智能合约空投
### 普通用户地址NEP5资产转账交易结构示例
```
{
    "jsonrpc": "2.0",
    "id": 3,
    "result": {
        "txid": "0x442a85d2898f3cf340163451295c87128d6acd96f53e167f9b38986368427767",
        "size": 219,
        "type": "InvocationTransaction",
        "version": 1,
        "attributes": [
            {
                "usage": "Script",
                "data": "666f6214f9d89e423d2c40cbdd3a2bf9d949761b"
            }
        ],
        "vin": [],
        "vout": [],
        "sys_fee": "0",
        "net_fee": "0",
        "scripts": [
            {
                "invocation": "406aa2d8c8dc13af1b917aa0fa4afba418689757d6e7813486c660bc3174aae0c2756d985d740b88b3daff9891bd885e395547b64ec13fc4930f4ad71a76ed2051",
                "verification": "21028dbd01171c54c92dee66bd9da05baca3705dbe3c53ef0730438e83e243f439bbac"
            }
        ],
        "script": "040065cd1d1401584b60b5cc675fb7ce72974bd2310ce47079f714666f6214f9d89e423d2c40cbdd3a2bf9d949761b53c1087472616e73666572675f89105fdcf8b97739c68f3e969d60b6e98bfa06f166547f0d917ec1ab05",
        "gas": "0",
        "blockhash": "0xce6a831ddcc73406d3325062c30b66561b695b36c0eeae8075aa06e80aa1a505",
        "confirmations": 279667,
        "blocktime": 1525789746
    }
}
```
这里有两段脚本结构：  
一段是一个数组scripts，包含了见证者的鉴权部分；  
另一个script是真正的执行操作040065cd1d1401584b60b5cc675fb7ce72974bd2310ce47079f714666f6214f9d89e423d2c40cbdd3a2bf9d949761b53c1087472616e73666572675f89105fdcf8b97739c68f3e969d60b6e98bfa06f166547f0d917ec1ab05这段执行脚本就是就是调用了5f89105fdcf8b97739c68f3e969d60b6e98bfa06f166合约进行转账。

### 先看一段NEP5 token的合约代码：  
```
public static bool Transfer(byte[] from, byte[] to, BigInteger amount)
{
	if (from.Length != 20 || to.Length != 20) return false;
	if (amount <= 0) return false;
	if (!Runtime.CheckWitness(from)) return false;
	if (from == to) return true;
	//省略部分代码
	return true;
}
``` 
 注意里面的这句代码
```
 if (!Runtime.CheckWitness(from)) return false;
```
这句代码校验了 from 这个地址是否见证了当前这笔交易，对应到这个具体的例子，这个from实际就是空投的智能合约，是一个合约。这就要面对以下两个问题：
1. 小蚁里鉴权和执行是分离的
2. 智能合约没有私钥  
  
对于一个普通用户地址，在操作自己的NEP5资产的时候，实际是用自己的私钥对整个交易签名，而执行脚本本身是整个交易的一部分，因此操作转账的执行脚本是被私钥签名过的，也就是所有者确实见证认同了这笔交易。
但对于一个智能合约，第一它没有私钥，第二作为空投这样的智能合约，人人都能触发这个合约，如何保证在鉴权通过后，都是诚实交易，不会多取。  
    
这里我们的思路：
1. 第一步身处见证区的智能合约脚本要能拿到执行脚本，在鉴权阶段对交易里的执行脚本进行校验；其实小蚁系统里，对于智能合约，鉴权阶段的入参就取执行脚本似乎更合理，整个逻辑会跟趋近以太坊等其他智能合约系统。
2. 第二步在后续执行阶段，能偶资产转移的部分，必须限定在合约的一段逻辑代码里，在这个逻辑范围内进一步保证空投的分配方式是合乎约定的。  

```C#
if (Runtime.Trigger == TriggerType.Verification)
{
	if (Runtime.CheckWitness(SuperAdmin)) return true;

	Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
	var type = tx.Type;

	 if (type != INVOCATION_TRANSACTION_TYPE) return false;

	var invocationTransaction = (InvocationTransaction)tx;
	if (invocationTransaction.Script.Length != 61)
	 {
		return false;
	}

	 if (invocationTransaction.Script[0] != 0x1c) return false;

	 if (invocationTransaction.Script.Range(29, 32) != (new byte[] { 0x51, 0xc1, 0x08 }).Concat("withdraw".AsByteArray()).Concat(new byte[] { 0x67 }).Concat(ExecutionEngine.ExecutingScriptHash))
	{
		return false;
	}
	return true;
}
```

