# How Claim LRN

We assume you have a basic understanding of how NEO works and the NEO RPC services; otherwise, this will be challanging for you.

There are two steps to claim LRN:

1. Query your claimable LRC balance
2. Trigger the withdrawal

## Query Balance

### Request

Take address `Ad3aF5yDcYy5N42wbegJrdVfjQy4SfTKfK` as an example，the little endian encoded **scriptHash** is `e952c4dde16eb4d176ad9efcd8ef5896588b746f`，please use the hash to replace `e952c4dde16eb4d176ad9efcd8ef5896588b746f` in script below. 

```
{
  "jsonrpc": "2.0",
  "method": "invokescript",
  "params": ["14e952c4dde16eb4d176ad9efcd8ef5896588b746f51c1157175657279417661696c61626c6542616c616e636567f7c5643ab1896195b8abe8cfd2e3b450441ca45c",1],
  "id": 3
}
```
### Response

The following is an example of the response. If the value is greater than 0, you have some LRC to claim.
```
{
    "jsonrpc": "2.0",
    "id": 3,
    "result": {
        "script": "14e952c4dde16eb4d176ad9efcd8ef5896588b746f51c1157175657279417661696c61626c6542616c616e636567f7c5643ab1896195b8abe8cfd2e3b450441ca45c",
        "state": "HALT, BREAK",
        "gas_consumed": "1.211",
        "stack": [
            {
                "type": "Integer",
                "value": "1844115397260"
            }
        ]
    }
}
```


## Withdrawal

Please check out the [sendrawtransaction doc](http://docs.neo.org/zh-cn/node/cli/2.7.4/api/sendrawtransaction.html) for how to send raw transactions. And here is an [example](https://neotracker.io/tx/de8b5e8dcd601ff3ec8ebb5c9835fb7ac002650db32601bb92d772f6088d4ee5) for LRN withdrawal.

The script to run is `1c2018072413270000a60f148dd6df43259886e10e4fd26d896e2394eb51c108776974686472617767f7c5643ab1896195b8abe8cfd2e3b450441ca45c`.
You need to replace `2018072413270000a60f148dd6df43259886e10e4fd26d896e2394eb` with something new. The first 8 bytes (`2018072413270000`) is a random number; the remaining part (`a60f148dd6df43259886e10e4fd26d896e2394eb`) is your address's scriptHash. Neo changed its fee model in 2019 so all transactions greater than 1024 bytes need to pay fees. To reduce the transaction size, authorization scripts are no longer provided in transactions. Please check out this [transaciton](https://neotracker.io/tx/43b530e62377ad4c5b5b2b6468963582ccbbfa4b4cab239a09e1666a4dd98039) for more information. Please read NEO's documentation for more informaiton or contact NEO.

## The airdrop is 100% contract based

LRN airdrop is 100% smart contract-based. Loopring used to provide an easy-to-use UI for the claiming, but we stopped the service to reduce cost. If you run into issues, please contact NEO for technical support.
