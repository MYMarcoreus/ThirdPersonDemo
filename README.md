# Unity客户端DEMO

## 项目功能

本客户端基于Unity，配合自研分布式游戏服务端（GateServer / AccountServer / CenterServer / LogicServer）进行多人房间制游戏交互。

1. 用户登录/注册
2. 房间管理（创建/搜索/加入/退出）
3. 场景登录与游戏内操作
4. 断线重连与数据同步
5. 消息广播接收

### 1️⃣ 登录与注册模块

#### 登录流程

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant GateServer
    participant AccountServer

    Client->>GateServer: LoginReq(username, password)
    GateServer->>AccountServer: RPC LoginReq
    AccountServer-->>GateServer: LoginRsp(success, uid, user_token)
    GateServer-->>Client: LoginRsp(success, uid, user_token)

    note right of Client: 客户端收到user_token后保存<br>用于后续场景登录
```

#### 注册流程

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant GateServer
    participant AccountServer

    Client->>GateServer: RegisterReq(username, password)
    GateServer->>AccountServer: RPC RegisterReq
    AccountServer-->>GateServer: RegisterRsp(success, uid)
    GateServer-->>Client: RegisterRsp(success, uid)
```

### 2️⃣ 房间管理模块

#### 创建房间

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant GateServer
    participant CenterServer
    participant LogicServer

    Client->>GateServer: CreateRoomReq
    GateServer->>CenterServer: CreateRoomReq
    CenterServer->>LogicServer: NewRoomReq
    LogicServer-->>CenterServer: NewRoomRsp(IP/端口)
    CenterServer-->>GateServer: CreateRoomRsp
    GateServer-->>Client: CreateRoomRsp(房间信息)
```

#### 搜索房间

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant GateServer
    participant CenterServer

    Client->>GateServer: SearchRoomReq
    GateServer->>CenterServer: SearchRoomReq
    CenterServer-->>GateServer: SearchRoomRsp(房间列表)
    GateServer-->>Client: SearchRoomRsp
```

#### 加入房间

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant GateServer
    participant CenterServer

    Client->>GateServer: JoinRoomReq
    GateServer->>CenterServer: JoinRoomReq
    CenterServer-->>GateServer: JoinRoomRsp
    GateServer-->>Client: JoinRoomRsp

    note right of Client: 收到其他玩家加入的广播消息<br>更新本地房间玩家列表
```

#### 退出房间

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant GateServer
    participant CenterServer

    Client->>GateServer: QuitRoomReq
    GateServer->>CenterServer: QuitRoomReq
    CenterServer-->>GateServer: QuitRoomRsp
    GateServer-->>Client: QuitRoomRsp

    note right of Client: 收到其他玩家退出的广播消息<br>更新本地房间玩家列表
```

### 3️⃣ 场景登录与游戏内操作

#### 获取逻辑服场景令牌

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant GateServer
    participant CenterServer

    Client->>GateServer: GetEnterSceneTokenReq
    GateServer->>CenterServer: GetEnterSceneTokenReq
    CenterServer-->>GateServer: GetEnterSceneTokenRsp(scene_token)
    GateServer-->>Client: GetEnterSceneTokenRsp(scene_token)
```

#### 逻辑服登录（Token 验证）

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant LogicServer
    participant Redis

    Client->>LogicServer: SceneLoginReq(user_token, scene_token)
    LogicServer->>Redis: 验证 token
    alt Token 验证成功
        LogicServer-->>Client: SceneLoginRsp(success)
    else 验证失败
        LogicServer-->>Client: SceneLoginRsp(fail)
    end
```

#### 游戏内操作与消息同步

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant LogicServer
    participant RoomManager
    participant Thread

    Client->>LogicServer: 玩家操作消息（移动/跳跃/攻击等）
    LogicServer->>RoomManager: 投递到房间消息队列
    RoomManager->>Thread: 房间事件处理
    Thread->>LogicServer: 向房间内其他玩家广播消息
    LogicServer-->>Client: 接收其他玩家动作消息并更新场景
```

### 4️⃣ 广播消息接收

客户端接收中心服或逻辑服广播的房间消息（例如其他玩家加入/退出、游戏事件广播）：

```mermaid
sequenceDiagram
    autonumber
    participant CenterServer
    participant GateServer
    participant Client

    CenterServer->>GateServer: BroadcastRoom(房间消息)
    GateServer->>Client: 广播消息
    note right of Client: 客户端收到消息后更新房间/玩家状态
```

