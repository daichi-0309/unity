using UnityEngine;

/// <summary>
/// 敵の状態を表す列挙型
/// 敵の動作状態を一元管理するために使用
/// </summary>
public enum EnemyState
{
    /// <summary>
    /// 待機状態：プレイヤーを検出していない
    /// 敵は移動せず、アイドルアニメーションを再生
    /// </summary>
    Idle,

    /// <summary>
    /// 追跡状態：プレイヤーを検出して追跡中
    /// 敵はプレイヤー方向に移動し、攻撃範囲に入るまで継続
    /// </summary>
    Chasing,

    /// <summary>
    /// 攻撃状態：プレイヤーが攻撃範囲内
    /// 敵は移動を停止して攻撃アニメーションを再生
    /// </summary>
    Attacking,

    /// <summary>
    /// ノックバック状態：ダメージを受けて吹き飛んでいる
    /// 敵は移動を停止し、EnemyKnockback コンポーネントが制御
    /// スタン時間が終了すると Idle に戻る
    /// </summary>
    Knockback
}
