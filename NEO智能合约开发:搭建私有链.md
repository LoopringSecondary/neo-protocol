# NEO智能合约开发:搭建私有链

  

**前言：**

​	NEO 私有链的部署至少需要 4 台服务器才能取得共识，每台服务器对应一个共识节点。

配置：

- ##### 四个虚拟机

- ##### 四个不同的静态ip

- ##### 200G的内存空间

  
小提示；推荐使用parallels desktop来配置虚拟机。



一、搭建私有链的步骤；

​	首先准备好要安装某系统的iso影像，利用iso影像安装一个系统之后再克隆3个虚拟机，虚拟机默认的上网方式是默认适配器，此时，您的网络是正常的，但是外部的地址和虚拟机无法通讯的，只有改变了虚拟机的mac物理地址并且关闭专用网络的防火墙，才能和外部地址进行通讯，但是电脑经过重新启动之后，因为虚拟机的物理地址和主机发生了冲突，导致虚拟机的网络连接不上。

解决办法；

​	打开parallels desktop控制中心、在您想配置虚拟机的地方生成新的Mac地址-然后关闭这个窗口-再打开进入虚拟机--禁用网卡再重启网卡，这样您的虚拟机即可以连接到网络又可以和外部地址进行通讯了。



二、常见的通讯问题；

​	但是这样的话，虽然可以保证可以完成私有链的搭建又可以和别人进行通讯，但是存在着一个很重要的问题；每次电脑重启的时候虚拟机的ip都会发生变化，而且如果4台虚拟机变了其中的一个，另外那三个都会变（即使剩下的3个ip没有发生冲突），结果导致您不但每次都要生成新的mac地址，而且还要修改私有链的配置文件，相对来说；比较麻烦！

建议及解决办法；

​	①.如果是个人独立去完成搭建私有链的话、你首先要保证 ”网络管理员” 或 “网络供应商” 提供给您4个固定的静态ip，这样的话就您就可以解决很多的麻烦！完全不用担心虚拟机的网络问题。

​	②.如果是个人完成私有链的话；您可以去云服务器上租用4个虚拟机，成本率高！

​	③.建议 2-3人以上来完成这个私有链的搭建,如果有 ”静态ip” 的条件这样最好，如果没有，那只能选择改变mac地址方式的这一办法，但是如果是多台电脑参与到私有链的搭建，这样既降低了ip冲突后变化的可能性，又不用担心电脑的运行速度，也不用担心虚拟机网络会挂（可能性很小），这样无疑是最好的办法了！

小提示；

​	每个虚拟机都要关闭专用网络的防火墙、不然虚拟机与虚拟机之间不能达成共识、另外要新建端口；防火墙--高级设置--入站规则--新建规则，然后分别添加端口 10331-10334，端口名gas。![img](https://lh3.googleusercontent.com/-U82ngkMbDt91qgAvZ-Iy33VMZTNTMeiGffOM6Qf1UxFxnHPOYQKRnUT3unKuLlIXf1SaU4KXei0A5plpa3F-BEk_Wr1ASnrW_9mBrQlNbCuD3VT7af9zxbFxklI1ZCALKQFhuMI)



三、下载安装Neo节点；

​	1.部署共识节点所用的是；neo-cli ，可以在 Windows、Linux 和 Docker 中运行https://github.com/neo-project/neo-cli/releases

注意；目前Neo节点在MacOS下不能正常运行

​	2.运行 NEO 节点需要安装 .NET Core Runtime，需要安装 2.0 或以上版本。windows直接下载并运行.Net Core即可Windows可能还需要安装Microsoft Visual C++ 2015 Redistributable Update 3（https://www.microsoft.com/en-us/download/details.aspx?id=53840）



四、创建钱包

​	我们首先创建 4 个钱包文件，依次为 wallet1.db3 - wallet4.db3，在 GUI客户端和cli命令行钱包都可以被创建(钱包名字是无所谓的，数字比较好记)，每台虚拟机上都创建了对应的钱包之后、创建好钱包后将 4 个钱包的公钥保存下来以备后用。直接复制上面的公钥或者用cli命令中的 list key 命令查看公钥再复制均可。

![img](https://lh6.googleusercontent.com/8hOGSmXCeUKNeYyvD3h6KFaZXaOxCcVzbbzQopO0_ak-bCLqvcqIiKEl7sEdpSNK2potFB7p7kRLhq-0KjUWvsiQYgsI8T4m3TJRcWJSrtSZYxyrK87_HOGATFM_FTw45itfyDU3)



五、修改节点的配置文件

​	打开cli节点所在的目录，找到配置文件 protocol.json 。首先修改 Magic 值，Magic 用于识别消息的来源网络，指定不同的 Magic 确保 NEO 区块链中的不同网络信息在传输过程中不会发送到其它网络。	

注意；Magic的类型是uint，所以注意所填写的数值是在；0-4294967295之间。然后修改 StandbyValidators，将第三步记下来的 4 个公钥填写在这里最后修改 SeedList，将第一步记下来的 IP 地址填写在这里，端口号保持不变。

​	最后将修改过的 protocol.json 复制到 4 个节点的cli和gui客户端目录下，替换之前的 protocol.json 的文件。然后在 4 台虚拟机上依次输入以下命令启动节点，打开钱包，开启共识。

​	例如我修改成下面的配置;

![img](https://lh4.googleusercontent.com/G3btCIOxRaSXSmlpNvw0ZhYXV8IYD1Z8nQcOFFByqM2Hpk10Qit__W_wd1EXKS3MQrccXm7Ok7mEx1ktg73TUg7YdAH5UDRt9tlpVoeyVX2eBIlZ5bfh8N8F49ck0VahwtX-TQpV)

![img](https://lh3.googleusercontent.com/cgRqUsKEsNh3Ql-8lHmfaOotWIW2I6MQiyeuF7D1DzCxIW8R_XRYHBYzHmrum483uZ0q5zSqjhJTTmPhKsOc_H5FIrEzYPNtqQPvYaXlmTPsychI-0Af3zXRU0tXCGctT49jKUQG)

六、启动共识

​	打开命令powershell窗口、cd进入到neo-cli所在目录

​	启动节点：

​	dotnet neo-cli.dll

​	打开钱包：

​	open wallet wallet1.db3

​	注：这里并非所有节点都要打开 wallet1 钱包，每个节点应该打开自己的钱包文件。

​	start consensus

如果上述操作成功，可以运行show state来查看节点状态。若高度发生变化，则说明共识过程正常。4个节点的共识过程如图所示；![img](https://lh5.googleusercontent.com/hURVQu3iSsrh1Ku_YCpVMx9e6oD4R3TfVIOV8FQdIstLNyPRkoN3tOpKOct0u6e8FuoHWl4VAtmHt4_r1Ax7ezq8j_erNOgwqG8a8phvaE0p9eDGFElhfCHJcZhTW97ey9sp9fKS)

4 个节点即使关掉一台依然可以达成共识![img](https://lh4.googleusercontent.com/jl9tb4HgNCFk4OwWkRYTPocSzxiax2_p9uyQDXw71hKJp6lVc4g6EZoZ6kvZrGp2Hh7v0v6eaWClyvIRg0nXT2KI2OXqh8pCA0P-nJusNif7tmAmrSYgIOsVyxU7GBRrMsJW5eAe)

当relay block并产生了块、说明高度再涨，已经达成共识那么恭喜您，您已经成功了完成私有链的搭建。

小提示；

私链每次退出的时候要exit正常退出，否则私有链在运行过程中，节点没有正常退出，有一部分几率会造成私链存放数据的Chain文件会受损！受损会导致之前的数据用不了，需要重新搭建私链！建议各位虚拟机的屏幕设置为常亮。（因为虚拟机长时间不操作会自动休眠而不工作的）



七、提取 NEO、NeoGas

**安装 PC 版客户端（Neo-GUI），修改配置文件 protocol.json 使其连接到私有链中。**

打开钱包，如果左下角有连接数不为零，而且一直在同步区块，表示该客户端已经成功地连接到了私有链中。

在 PC 版客户端中打开钱包 wallet1.db3，添加多方签名地址，输入 protocol.json 中的 4 个公钥，设置最小签名数量为 3（共识节点数量 / 2 + 1），每一个节点都要重复这样的操作，如图所示;![img](https://lh6.googleusercontent.com/eKGLC1HdAMosyuncugHNw-IOpxuafWXfiuQN1WU_dGdLfjENa6mtJznOQlMIXgd3LrWjZm5qJnnh1NbdhMM_68ZU-gyWJv8LWAqsWItdWhJbDmePnDoyxZDI8tPz9AY_1Hbcqc7e)

确定，然后需要重建钱包索引，在菜单栏中点击 钱包 重建钱包索引 然后你就会看到在合约地址中有 1 亿的 NEO 了。![img](https://lh3.googleusercontent.com/2osNoTZdyEjAKQWKRui8jZzLEuObwZJpUPbcBgAbFf-xWXsqxDYocogKu0VlbKfBiriSnDtAQ2tOtpwx_iMDwt7CwBgbyBj7ZM_Llc5d8ocWdamqjWCGn9nhlkkF57eTHWJJGqnn)

下面我们要将该 NEO 从合约地址转到普通地址中，打开 4 个钱包中的任意一个，点击 交易 转账 输入一个标准地址，将 1 亿 NEO 转到这个地址中。

然后系统会提示“交易构造完成，但没有足够的签名”，然后将代码复制下来，打开第二个钱包，点击 交易 签名粘贴刚才复制的代码，点击 签名， 然后将代码复制下来，打开第三个钱包，点击 交易 签名 粘贴刚才复制的代码，点击 签名， 这时你会发现窗口中出现了一个 广播 按钮，代表交易已经签名完成（达到多方签名合约要求的最少签名数量）可以广播，点击 广播 后转账交易开始广播，约 15 秒后转账成功。![img](https://lh6.googleusercontent.com/3TV3BQfV999JdEN1tGhik003YX0hIdkzaYVGqFPD1EkUymv7cGvxt5B66NQBeu1bsCy3paC-Q4lYLvkCvJjgGjOviupABBToMCZFP8zQv0VtrTOqFUEDOcQFyFirJexPY_Mwl7lG)

提取 NeoGas 的操作方法也类似，点击 高级 提取 NeoGas 提取 ；![img](https://lh4.googleusercontent.com/0wkU_T_Ul_JNZqZUv7vlta3x0LDaoPYDsaGUGCCZwPt0kQ4Ea5wwESjqz5nAPTXtww22uZDmBqyTrInAoVtxNJ6HwyXdG3XpDYE3DYEkdVoEtyepI7zHeOikiP6ndpeLZGrzU2H0)

**接下来的操作与转账 NEO 类似，将没有足够的签名的代码复制下来，打开第二个钱包，点击 交易 签名 粘贴刚才复制的代码，点击 签名， 然后将代码复制下来，打开第三个钱包，点击 交易 签名 粘贴刚才复制的代码，点击 签名，点击 广播 后提取 NeoGas 的交易开始广播，约 15 秒后提取成功。**

提取成功后 NeoGas 会进入到你发起提取 NeoGas 的交易所在的钱包（即上方的 X 钱包）的第一个标准地址。![img](https://lh6.googleusercontent.com/KSzH5C9o7hUmDPjN0vVq_NymVDmHO35-DEmNOxO2XmRe05rC93X-7tSF3Q8xJoNL5GY22OfjHcFRZ5anilhTBXgmUx16-MR-V1z2QP8eLA7W95RW7OBP-xswjDvxdKY9dxmPdTmL)



八、如何把不可提取的gas转换为可提取；

​	打开neo-cli，输入命令；send +地址 +转入gas的数量

​	再打开GUI就可看到可提取的gas数量了。



九、总结；

​	总的来说；搭建私有链的过程中看似简单，但是中间的过程其实是有很多让人不能理解的地方、有很多弯路，存在着很多坑，尤其是配置网络的环境和私有链运行过程中的一些bug！

parallels desktop的破解版

​	parallels desktop下载地址（破解版）；    ------百度网盘---链接:     https://pan.baidu.com/s/17vZLYqD8ZRLICknF8wJidg   密码: ze77
