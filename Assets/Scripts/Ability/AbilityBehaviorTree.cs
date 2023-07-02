using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NodeCanvas.Tasks.Actions;
using UnityEditor;
using UnityEngine;

namespace Ability
{
    /// <summary>
    /// 技能行为树
    ///                                                               -> AbilityAction 
    /// 管理关系：AbilityBehaviorTree -> AbilityNode -> AbilityBehavior 
    ///                                                               -> AbilityCondition
    /// </summary>
    public class AbilityBehaviorTree : ILogic
    {
        /// <summary>
        /// 当前行为的帧计数
        /// </summary>
        int curFrame;
        /// <summary>
        /// 当前进行的行为节点
        /// </summary>
        AbilityBehavior curBehavior;
        /// <summary>
        /// 当前执行的行为节点的索引（这里Index和Id是相等的）
        /// </summary>
        int curNodeIndex;
        List<AbilityNode> nodeList = new();
        List<AbilityBehavior> behaviorsList = new();

        float fps;
        float cacheTime;
        /// <summary>
        /// 当前动作是否可以取消
        /// </summary>
        public bool CanCancel;
        ActorModel actorModel;

        public AbilityBehaviorTree(ActorModel model)
        {
            actorModel = model;
            fps = 1.0f / GameManager_Settings.TargetFraneRate;
        }

        public void Init() { }

        public void Init(string nodePath, string behaviorPath)
        {

            LoadBehavior(behaviorPath);
            LoadNode(nodePath);

            StartBehavior(GetBehavior("Default"));
            // curBehavior = GetBehavior("Default");
        }

        private void LoadNode(string nodePath)
        {
            nodeList = Resources.LoadAll<AbilityNode>(nodePath).ToList();
            if (nodeList.Count == 0)
            {
                Debug.LogError("行为节点初始化错误");
                return;
            }
            nodeList.Sort((x, y) => x.Id.CompareTo(y.Id));

            //设置Node和Behavior的对应关系 
            var name2Index = new Dictionary<string, int>();
            for (int i = 0; i < behaviorsList.Count; i++)
            {
                name2Index[behaviorsList[i].name] = i;
            }
            foreach (var item in nodeList)
            {
                var nameT = Regex.Replace(item.name, @"\d", ""); // Dash1，Dash2，Dash3 只对比Dash
                var isGet = name2Index.TryGetValue(item.name, out int index) || name2Index.TryGetValue(nameT, out index);
                if (isGet)
                {
                    item.BehaviorIndex = index;
                }
                else
                {
                    Debug.LogError($"设置Node和Behavior的对应关系错误 {item.name}");
                }
            }
        }

        private void LoadBehavior(string behaviorPath)
        {
            behaviorsList = Resources.LoadAll<AbilityBehavior>(behaviorPath).ToList();
            if (behaviorsList.Count == 0)
            {
                Debug.LogError("行为数据初始化错误");
                return;
            }

            foreach (var behavior in behaviorsList)
            {
                behavior.Init(this);
                foreach (var action in behavior.Actions)
                {
                    action?.Init(this);
                }
            }
        }

        public void Enter()
        {
        }

        public void Exit()
        {
        }

        public void Tick()
        {
            var nextBehavior = TryGetNextBehavior();
            if (nextBehavior != null)
            {
                StartBehavior(nextBehavior);
            }

            cacheTime += Time.deltaTime;

            // 超过fps执行一次Tick
            while (cacheTime > fps)
            {
                curBehavior.Tick(curFrame);
                curFrame += 1;
                Debugger.Log($"{curFrame}", LogDomain.Frame);

                // 执行次数？生命周期完整？重置之后curFrame是否正确？
                if (curFrame > curBehavior.FrameLength)
                {
                    if (curBehavior.IsLoop)
                    {
                        LoopBehavior();
                    }
                    else
                    {
                        EndBehavior();
                    }
                }

                cacheTime -= fps;
            }
        }


        /// <summary>
        /// 将行为重置到第一帧
        /// </summary>
        private void LoopBehavior()
        {
            curFrame = 1;
        }

        private void EndBehavior()
        {
            StartBehavior(GetBehavior("Default"));
        }

        private AbilityBehavior GetBehavior(string name)
        {
            foreach (var item in behaviorsList)
            {
                if (item.name == name)
                {
                    return item;
                }
            }

            Debug.LogError($"行为不存在 {name}");
            return null;
        }

        private AbilityBehavior TryGetNextBehavior()
        {
            if (nodeList.Count == 0)
            {
                Debug.LogError($"没有可选择的行为节点");
                return null;
            }

            if (curNodeIndex >= nodeList.Count)
            {
                curNodeIndex = 0;
            }

            AbilityNode curNode = nodeList[curNodeIndex];
            int priority = -1;
            AbilityNode nextNode = default;
            foreach (var newNodeIndex in curNode.Childs)
            {
                AbilityNode newNode = nodeList[newNodeIndex];
                AbilityBehavior behavior = behaviorsList[newNode.BehaviorIndex];
                // 检查输入
                if (GameManager_Input.Instance.bufferKeys.Any(predicate => predicate == behavior.InputKey))
                {
                    // 检查条件
                    if (newNode.CheckCondition(this))
                    {
                        if (newNode.Priority > priority)
                        {
                            priority = newNode.Priority;
                            nextNode = newNode;
                        }
                    }
                }
            }
            if (priority > -1)
            {
                curNodeIndex = nextNode.BehaviorIndex;
                var newBehavior = behaviorsList[curNodeIndex];
                // Debugger.Log($"切换行为 {newBehavior.name}", LogDomain.AbilityBehavior);
                return newBehavior;
            }

            return null;
        }

        public void StartBehavior(AbilityBehavior newBehavior)
        {
            if (newBehavior == null)
                return;

            curBehavior?.Exit();
            curFrame = 1;
            curBehavior = newBehavior;
            newBehavior.Enter();

            if (curBehavior == GetBehavior("Default"))
            {
                curNodeIndex = 0;
            }
            CanCancel = false;
            // Debugger.Log($"开始行为 {curBehavior.name}", LogDomain.AbilityBehavior);
        }

        private void ResetBehavior(AbilityBehavior behavior)
        {
            foreach (var item in behavior.Actions)
            {
                item.Exit();
            }
        }

    }
}

