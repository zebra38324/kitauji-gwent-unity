using LanguageExt.TypeClasses;
using System;
using System.Collections;
using System.Collections.Generic;

public class AIBase
{
    public enum AILevel
    {
        L1 = 0, // 不考虑手牌具体收益，线性概率分布选择
        L2, // 考虑手牌后续收益，指数概率分布选择
    }

    public enum AIMode
    {
        Normal = 0, // 普通模式，进行一定复杂度的计算
        Quick, // 快速模式
        Test, // 测试模式，不进行await
    }

    // 按照线性概率（降序），获取0~(total-1)的随机数
    public static int GetIndexLinearProbabilities(int num)
    {
        double total = num * (num + 1) / 2.0;
        List<double> probabilities = new List<double>();
        for (int i = num; i >= 1; i--) {
            probabilities.Add(i / total);
        }
        double randomValue = new Random().NextDouble(); // 生成一个0到1之间的随机数
        double cumulativeProbability = 0.0;
        for (int i = 0; i < num; i++) {
            cumulativeProbability += probabilities[i];
            if (randomValue <= cumulativeProbability) {
                return i;
            }
        }
        return 0;
    }

    // 按照指数概率（降序），获取0~(total-1)的随机数
    // num不能过大，否则可能溢出
    public static int GetIndexExponentialProbabilities(int num)
    {
        int total = 0;
        int cur = 1;
        List<int> probabilities = new List<int>();
        for (int i = 0; i < num; i++) {
            probabilities.Add(cur);
            total += cur;
            cur *= 2; // 指数增长
        }
        probabilities.Reverse();
        int randomValue = new Random().Next(0, total);
        int curSum = 0;
        for (int i = 0; i < num; i++) {
            curSum += probabilities[i];
            if (randomValue < curSum) {
                return i;
            }
        }
        return 0;
    }
}
