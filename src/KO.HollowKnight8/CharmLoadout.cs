using System;
using System.Collections.Generic;
using System.Linq;

namespace KO.HollowKnight8;

internal sealed class CharmLoadout
{
	internal readonly bool[] Equipped = new bool[40];

	internal List<int> Order = new List<int>();

	internal int Used;

	internal bool Overcharmed;

	internal void Read(PlayerData data)
	{
		Equipped[0] = data.equippedCharm_1;
		Equipped[1] = data.equippedCharm_2;
		Equipped[2] = data.equippedCharm_3;
		Equipped[3] = data.equippedCharm_4;
		Equipped[4] = data.equippedCharm_5;
		Equipped[5] = data.equippedCharm_6;
		Equipped[6] = data.equippedCharm_7;
		Equipped[7] = data.equippedCharm_8;
		Equipped[8] = data.equippedCharm_9;
		Equipped[9] = data.equippedCharm_10;
		Equipped[10] = data.equippedCharm_11;
		Equipped[11] = data.equippedCharm_12;
		Equipped[12] = data.equippedCharm_13;
		Equipped[13] = data.equippedCharm_14;
		Equipped[14] = data.equippedCharm_15;
		Equipped[15] = data.equippedCharm_16;
		Equipped[16] = data.equippedCharm_17;
		Equipped[17] = data.equippedCharm_18;
		Equipped[18] = data.equippedCharm_19;
		Equipped[19] = data.equippedCharm_20;
		Equipped[20] = data.equippedCharm_21;
		Equipped[21] = data.equippedCharm_22;
		Equipped[22] = data.equippedCharm_23;
		Equipped[23] = data.equippedCharm_24;
		Equipped[24] = data.equippedCharm_25;
		Equipped[25] = data.equippedCharm_26;
		Equipped[26] = data.equippedCharm_27;
		Equipped[27] = data.equippedCharm_28;
		Equipped[28] = data.equippedCharm_29;
		Equipped[29] = data.equippedCharm_30;
		Equipped[30] = data.equippedCharm_31;
		Equipped[31] = data.equippedCharm_32;
		Equipped[32] = data.equippedCharm_33;
		Equipped[33] = data.equippedCharm_34;
		Equipped[34] = data.equippedCharm_35;
		Equipped[35] = data.equippedCharm_36;
		Equipped[36] = data.equippedCharm_37;
		Equipped[37] = data.equippedCharm_38;
		Equipped[38] = data.equippedCharm_39;
		Equipped[39] = data.equippedCharm_40;
		Order = data.equippedCharms ?? new List<int>();
		Used = data.charmSlotsFilled;
		Overcharmed = data.overcharmed;
	}

	internal void Write(PlayerData data)
	{
		data.equippedCharm_1 = Equipped[0];
		data.equippedCharm_2 = Equipped[1];
		data.equippedCharm_3 = Equipped[2];
		data.equippedCharm_4 = Equipped[3];
		data.equippedCharm_5 = Equipped[4];
		data.equippedCharm_6 = Equipped[5];
		data.equippedCharm_7 = Equipped[6];
		data.equippedCharm_8 = Equipped[7];
		data.equippedCharm_9 = Equipped[8];
		data.equippedCharm_10 = Equipped[9];
		data.equippedCharm_11 = Equipped[10];
		data.equippedCharm_12 = Equipped[11];
		data.equippedCharm_13 = Equipped[12];
		data.equippedCharm_14 = Equipped[13];
		data.equippedCharm_15 = Equipped[14];
		data.equippedCharm_16 = Equipped[15];
		data.equippedCharm_17 = Equipped[16];
		data.equippedCharm_18 = Equipped[17];
		data.equippedCharm_19 = Equipped[18];
		data.equippedCharm_20 = Equipped[19];
		data.equippedCharm_21 = Equipped[20];
		data.equippedCharm_22 = Equipped[21];
		data.equippedCharm_23 = Equipped[22];
		data.equippedCharm_24 = Equipped[23];
		data.equippedCharm_25 = Equipped[24];
		data.equippedCharm_26 = Equipped[25];
		data.equippedCharm_27 = Equipped[26];
		data.equippedCharm_28 = Equipped[27];
		data.equippedCharm_29 = Equipped[28];
		data.equippedCharm_30 = Equipped[29];
		data.equippedCharm_31 = Equipped[30];
		data.equippedCharm_32 = Equipped[31];
		data.equippedCharm_33 = Equipped[32];
		data.equippedCharm_34 = Equipped[33];
		data.equippedCharm_35 = Equipped[34];
		data.equippedCharm_36 = Equipped[35];
		data.equippedCharm_37 = Equipped[36];
		data.equippedCharm_38 = Equipped[37];
		data.equippedCharm_39 = Equipped[38];
		data.equippedCharm_40 = Equipped[39];
		data.equippedCharms = Order;
		data.charmSlotsFilled = Used;
		data.overcharmed = Overcharmed;
	}

	internal int[] Ids()
	{
		return (from i in Enumerable.Range(1, 40)
			where Equipped[i - 1]
			select i).ToArray();
	}

	internal void Initialize(PlayerData data, int[] ids)
	{
		Array.Clear(Equipped, 0, 40);
		Order = new List<int>();
		Used = 0;
		int[] array = ids ?? new int[0];
		for (int i = 0; i < array.Length; i++)
		{
			int num = array[i];
			if (num >= 1 && num <= 40 && !Equipped[num - 1] && data.GetBool("gotCharm_" + num))
			{
				int num2 = Math.Max(0, data.GetInt("charmCost_" + num));
				if (CoopRules.CanEquip(Used, num2, data.charmSlots, data.canOvercharm))
				{
					Equipped[num - 1] = true;
					Order.Add(num);
					Used += num2;
				}
			}
		}
		Overcharmed = Used > data.charmSlots;
	}
}
