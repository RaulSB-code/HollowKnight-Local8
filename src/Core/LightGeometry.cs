namespace KO.HollowKnight8;

internal static class LightGeometry
{
	internal static int Clip(LightVertex[] input, int count, LightVertex[] output, float nx, float ny, float limit)
	{
		if (count == 0)
		{
			return 0;
		}
		int result = 0;
		LightVertex lightVertex = input[count - 1];
		float num = nx * lightVertex.X + ny * lightVertex.Y - limit;
		for (int i = 0; i < count; i++)
		{
			LightVertex lightVertex2 = input[i];
			float num2 = nx * lightVertex2.X + ny * lightVertex2.Y - limit;
			if (num <= 0f != num2 <= 0f)
			{
				float num3 = num / (num - num2);
				output[result++] = new LightVertex(lightVertex.X + (lightVertex2.X - lightVertex.X) * num3, lightVertex.Y + (lightVertex2.Y - lightVertex.Y) * num3, lightVertex.Z + (lightVertex2.Z - lightVertex.Z) * num3, lightVertex.U + (lightVertex2.U - lightVertex.U) * num3, lightVertex.V + (lightVertex2.V - lightVertex.V) * num3);
			}
			if (num2 <= 0f)
			{
				output[result++] = lightVertex2;
			}
			lightVertex = lightVertex2;
			num = num2;
		}
		return result;
	}
}
