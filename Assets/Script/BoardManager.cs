using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
	[System.Serializable]
	public struct PlayLine
	{
		public string name;
		public Vector2 pos;
		public Vector2 lenDir;
		public Vector2 playDir;
		public float len;
	}
	[SerializeField] PlayLine[] playLines;

	[Header("Board Config")]
	[SerializeField] float spawnCube = 6;
	[SerializeField] float boardCube = 7;
	[Space]
	[SerializeField] float playSideLen = 5;
	[SerializeField] int playSideChecks = 5;
	[SerializeField] float reflectSideLen = 6;
	[SerializeField] int reflectSideChecks = 9;
	[Space]
	[SerializeField] float strikerRadius = 0.37f;
	[SerializeField] float puckRadius = 0.26f;
	[Space]
	[SerializeField] float strikeThreshold = 0.5f;
	[SerializeField] float speedAdder = 1;
	[SerializeField] Vector2 speedMinMax = Vector2.one + Vector2.up * 49;

	[Header("Reference")]
	[SerializeField] Rigidbody2D striker;
	[SerializeField] Rigidbody2D[] pucks;
	[SerializeField] Transform[] pots;
	[SerializeField] Transform visualPot, visualPuck, visualStrike, visualTarget;
	[SerializeField] LineRenderer visualLine;

	[Header("Realtime (ReadOnly)")]
	[SerializeField] Transform tPuck;
	[SerializeField] Transform tPot;
	[SerializeField] Vector2 strikeDir = Vector2.up;
	[SerializeField] float strikeSpeed = 10;

	// Auto Reset will be Nice :)
	bool resetDone = false;
	private void LateUpdate()
	{
		if (!resetDone && striker.IsSleeping())
		{
			foreach (var p in pucks)
				if (!p.IsSleeping() && p.gameObject.activeSelf)
					return;
			resetDone = true;
			UI_Calculate();
		}
	}

	#region AI_Calculations
	[System.Serializable]
	public struct StrikeInfo
	{
		public int potID;
		public int puckID;
		public float hDot;
		public Vector2 pos;
		public Vector2 dir;
		public Vector2 tar;
		public Vector2 rVec;
		public float speed;
	}
	[SerializeField] List<StrikeInfo> strikeInfo;
	Vector3 Get_NormalPoint_OnLine(Vector3 p, Vector3 a, Vector3 b)
	{
		Vector3 ap = p - a;
		Vector3 ab = b - a;
		ab.Normalize();
		ab *= Vector3.Dot(ap, ab);
		return a + ab;
	}
	public float? Get_Ray_OnLineSeg(Vector3 rayOrg, Vector3 rayDir, Vector3 point1, Vector3 point2)
	{
		var v1 = rayOrg - point1;
		var v2 = point2 - point1;
		var v3 = new Vector3(-rayDir.y, rayDir.x, 0);

		var dot = Vector2.Dot(v2, v3);
		if (Mathf.Abs(dot) < 0.000001f)
			return null;

		var rM = Vector3.Cross(v2, v1) / dot;
		var lM = Vector2.Dot(v1, v3) / dot;

		if (rM.z >= 0.0f && (lM >= 0.0f && lM <= 1.0f))
			return rM.z;

		return null;
	}
	private List<StrikeInfo> Calc_DirectStrike(PlayLine pLine, int lSplit, int potID, bool checkObstacles = true)
	{
		var hitInfo = new List<StrikeInfo>();
		Vector2 pot, puck;
		for (int p = 0; p < pucks.Length; p++)
		{
			if (pucks[p].gameObject.activeSelf)
			{
				puck = pucks[p].position;
				pot = pots[potID].position;

				// OBSTACLE Check : If Path is Clear from This PUCK to Target POT
				var puck2Pot = checkObstacles && (Physics2D.CircleCastAll(puck, puckRadius, (pot - puck).normalized, boardCube * 2, 1 << 9).Length > 1);
				if (puck2Pot)
					continue;

				//Some Platform Indipendent Vector Math, to get 
				var hitDir = (pot - puck);
				var dist = hitDir.magnitude;
				hitDir.Normalize();
				var hitPos = (puck - hitDir * (puckRadius + strikerRadius));

				// EDGE Check : If Position is Overlaping Edge, Adjust accordingly
				if (Physics2D.OverlapCircle(hitPos, strikerRadius, 1 << 11) != null)
				{
					var dx = Mathf.Min(Mathf.Abs(hitPos.x), boardCube - strikerRadius);
					var dy = Mathf.Min(Mathf.Abs(hitPos.y), boardCube - strikerRadius);
					hitPos.x = Mathf.Sign(hitPos.x) * dx;
					hitPos.y = Mathf.Sign(hitPos.y) * dy;
				}

				var info = new StrikeInfo(); // We will store the best Hit Vector for this PUCK-POT combo.
				for (int i = -1; i <= lSplit; i++) // to ccheck more than one possiblity to take the shot
				{
					Vector2 sPos;
					if (i < 0) // For Best possible shot
					{
						var rM = Get_Ray_OnLineSeg(hitPos, -hitDir, pLine.pos, pLine.pos + pLine.lenDir * pLine.len);
						if (rM != null && rM.HasValue)
							sPos = hitPos - hitDir * rM.Value;
						else
							continue;
					}
					else //when Best line of sight isn't possible, we will try for other positions
					{
						sPos = pLine.pos + pLine.lenDir * pLine.len * (i / (float)lSplit);
					}

					// OBSTACLE Check : If Position is Clear for Striker Placement (in case a puck is blocking)
					if (Physics2D.OverlapCircle(sPos, strikerRadius, 1 << 9) != null)
						continue;

					var hitVect = (hitPos - sPos);
					var hitDot = Vector2.Dot(hitDir, hitVect.normalized);
					var tSpeed = dist + hitVect.magnitude; //Linear Drag is 1, So X speed will cover X distance.
					var sDir = hitVect.normalized;
					tSpeed = (tSpeed / hitDot) + speedAdder;
					tSpeed = Mathf.Clamp(tSpeed, speedMinMax.x, speedMinMax.y);
					if (tSpeed < speedMinMax.y && hitDot > strikeThreshold 
						&& hitDot > info.hDot && Vector2.Dot(pLine.playDir, sDir) > 0)
					{
						//OBSTACLE Check :  If Path is Clear from This Position to This PUCK
						var striker2Puck = Physics2D.CircleCast(sPos, strikerRadius, sDir, boardCube * 2, 1 << 9);
						if (striker2Puck.rigidbody == pucks[p])
						{
							info.puckID = p;
							info.potID = potID;

							info.pos = sPos;
							info.dir = sDir;
							info.tar = hitPos;
							info.hDot = hitDot;
							info.speed = tSpeed;
						}
					}
				}
				if (info.speed != 0)
					hitInfo.Add(info);
			}
		}
		return hitInfo;
	}
	private List<StrikeInfo> Calc_ReflectedStrike(PlayLine rLine, int lSplit, int potID, PlayLine pLine, bool checkObstacles = true)
	{
		var hitInfo = new List<StrikeInfo>();
		Vector2 pot, puck;
		for (int p = 0; p < pucks.Length; p++)
		{
			if (pucks[p].gameObject.activeSelf)
			{
				puck = pucks[p].position;
				pot = pots[potID].position;

				// OBSTACLE Check : If Path is Clear from This PUCK to Target POT
				var puck2Pot = checkObstacles && (Physics2D.CircleCastAll(puck, puckRadius, (pot - puck).normalized, boardCube * 2, 1 << 9).Length > 1);
				if (puck2Pot)
					continue;

				//Some Platform Indipendent Vector Math, to get 
				var hitDir = (pot - puck);
				var dist = hitDir.magnitude;
				hitDir.Normalize();
				var hitPos = (puck - hitDir * (puckRadius + strikerRadius));
				// EDGE Check : If Position is Overlaping Edge, Adjust accordingly
				if (Physics2D.OverlapCircle(hitPos, strikerRadius, 1 << 11) != null)
				{
					var dx = Mathf.Min(Mathf.Abs(hitPos.x), boardCube - strikerRadius);
					var dy = Mathf.Min(Mathf.Abs(hitPos.y), boardCube - strikerRadius);
					hitPos.x = Mathf.Sign(hitPos.x) * dx;
					hitPos.y = Mathf.Sign(hitPos.y) * dy;
				}

				var info = new StrikeInfo(); // We will store the best Hit Vector for this PUCK-POT combo.
				for (int i = -1; i <= lSplit; i++) // to ccheck more than one possiblity to take the shot
				{
					Vector2 sPos;
					if (i < 0) // For Best possible shot
					{
						var rM = Get_Ray_OnLineSeg(hitPos, -hitDir, rLine.pos, rLine.pos + rLine.lenDir * rLine.len);
						if (rM != null && rM.HasValue)
							sPos = hitPos - hitDir * rM.Value;
						else
							continue;
					}
					else //when Best line of sight isn't possible, we will try for other positions
					{
						sPos = rLine.pos + rLine.lenDir * rLine.len * (i / (float)lSplit);
					}

					// OBSTACLE Check : If Position is Clear for Striker Placement (in case a puck is blocking)
					if (Physics2D.OverlapCircle(sPos, strikerRadius, 1 << 9) != null)
						continue;

					var hitVect = (hitPos - sPos);
					var hitDot = Vector2.Dot(hitDir, hitVect.normalized);
					var tSpeed = dist + hitVect.magnitude; //Linear Drag is 1, So X speed will cover X distance.
					var sDir = hitVect.normalized;
					tSpeed = (tSpeed / hitDot) + speedAdder;
					tSpeed = Mathf.Clamp(tSpeed, speedMinMax.x, speedMinMax.y);
					if (tSpeed < speedMinMax.y && hitDot > strikeThreshold 
						&& hitDot > info.hDot && Vector2.Dot(rLine.playDir, sDir) > 0)
					{
						//OBSTACLE Check :  If Path is Clear from This Position to This PUCK
						var Reflect2Puck = Physics2D.CircleCast(sPos, strikerRadius, sDir, boardCube * 2, 1 << 9);
						if (Reflect2Puck.rigidbody == pucks[p])
						{
							// Let's Check for Reflection THEN
							var reflect = Vector2.Reflect(-sDir, Vector2.Perpendicular(rLine.lenDir));
							var rM = Get_Ray_OnLineSeg(sPos, reflect, pLine.pos, pLine.pos + pLine.lenDir * pLine.len);
							if (rM != null && rM.HasValue)
							{
								tSpeed += rM.Value;
								if (tSpeed < speedMinMax.y && hitDot > strikeThreshold
									&& Vector2.Dot(pLine.playDir, -reflect) > 0)
								{
									var chkPos = sPos + reflect * rM.Value;
									var chkDir = -reflect;
									//OBSTACLE Check :  If Path is Clear from Striker to Refletion Point
									if (!Physics2D.CircleCast(chkPos, strikerRadius, chkDir, rM.Value, 1 << 9)
										&& Physics2D.OverlapCircle(chkPos, strikerRadius, 1 << 9) == null)
									{
										info.pos = chkPos;
										info.dir = chkDir;
										info.puckID = p;
										info.potID = potID;
										info.tar = sPos;
										info.rVec = hitVect;
										info.hDot = hitDot;
										info.speed = tSpeed;
									}
								}
							}
						}
					}
				}
				if (info.speed != 0)
					hitInfo.Add(info);
			}
		}
		return hitInfo;
	}

	private void Calc_BestShot(int pID, bool checkObstalces = true)
	{
		strikeSpeed = 0;
		strikeDir = Vector2.zero;
		striker.transform.position = Vector3.zero;
		striker.velocity = Vector2.zero;

		if (strikeInfo == null)
			strikeInfo = new List<StrikeInfo>();
		strikeInfo.Clear();

		int p0 = pID, p1 = (pID + 1) % 4, p2 = (pID + 2) % 4, p3 = (pID + 3) % 4;
		int b0 = ((pID + 1) % 4 + 4), s0 = (pID + 4), s1 = ((pID + 2) % 4 + 4);

		if (UI_DirectShot)
		{
			var d0p0 = Calc_DirectStrike(playLines[pID], playSideChecks, p0, checkObstalces);
			strikeInfo.AddRange(d0p0);
			var d0p1 = Calc_DirectStrike(playLines[pID], playSideChecks, p1, checkObstalces);
			strikeInfo.AddRange(d0p1);
		}

		if (UI_BackShot)
		{
			var b0p2 = Calc_ReflectedStrike(playLines[b0], reflectSideChecks, p2, playLines[pID], checkObstalces);
			strikeInfo.AddRange(b0p2);

			var b0p3 = Calc_ReflectedStrike(playLines[b0], reflectSideChecks, p3, playLines[pID], checkObstalces);
			strikeInfo.AddRange(b0p3);
		}

		if (UI_SideShot)
		{
			var s0p1 = Calc_ReflectedStrike(playLines[s0], reflectSideChecks, p1, playLines[pID], checkObstalces);
			strikeInfo.AddRange(s0p1);

			var s1p0 = Calc_ReflectedStrike(playLines[s1], reflectSideChecks, p0, playLines[pID], checkObstalces);
			strikeInfo.AddRange(s1p0);
		}

		if (strikeInfo.Count > 0)
		{
			int bestStrike = 0;
			float compBase = 0;
			for (int s = 0; s < strikeInfo.Count; s++)
			{
				if (compBase < strikeInfo[s].hDot)
				{
					compBase = strikeInfo[s].hDot;
					bestStrike = s;
				}
			}
			strikeDir = strikeInfo[bestStrike].dir;
			strikeSpeed = strikeInfo[bestStrike].speed;
			striker.transform.position = strikeInfo[bestStrike].pos;
			striker.velocity = Vector2.zero;

			//Visualize
			Visualize_Strike(bestStrike);
		}
		else if (checkObstalces)
		{
			// If no shot possible without collision, then let's HIT anyway
			Calc_BestShot(pID, false); //Recursion is not a bad idea for this case.
		}else
		{
			visualPot.gameObject.SetActive(false);
			visualPuck.gameObject.SetActive(false);
			visualLine.gameObject.SetActive(false);
			visualStrike.gameObject.SetActive(false);
			visualTarget.gameObject.SetActive(false);
			Visualize_Strike(-1);
		}
	}

	void Visualize_Strike(int id)
	{
		if (id < 0 || id >= strikeInfo.Count)
		{
			visualPot.gameObject.SetActive(false);
			visualPuck.gameObject.SetActive(false);
			visualLine.gameObject.SetActive(false);
			visualStrike.gameObject.SetActive(false);
			visualTarget.gameObject.SetActive(false);
			striker.transform.position = Vector2.zero;
			return;
		}
		//Visualize
		striker.transform.position = strikeInfo[id].pos;
		visualPot.gameObject.SetActive(true);
		visualPuck.gameObject.SetActive(true);
		visualLine.gameObject.SetActive(true);
		visualStrike.gameObject.SetActive(true);
		visualTarget.gameObject.SetActive(true);
		visualStrike.up = strikeInfo[id].dir;
		visualStrike.position = strikeInfo[id].pos;
		visualTarget.position = strikeInfo[id].tar;
		visualPot.position = pots[strikeInfo[id].potID].position;
		visualPuck.position = pucks[strikeInfo[id].puckID].position;
		visualPuck.up = (visualPot.position - visualPuck.position);
		visualPot.up = visualPuck.up;

		if (visualLine.positionCount != 4)
			visualLine.positionCount = 4;
		visualLine.SetPosition(0, visualStrike.position);
		visualLine.SetPosition(1, visualTarget.position);
		visualLine.SetPosition(2, visualPuck.position);
		visualLine.SetPosition(3, visualPot.position);
	}
	#endregion

	#region GamePlayFunc
	private void Take_Shot()
	{
		striker.gameObject.SetActive(true);
		striker.velocity = strikeDir * strikeSpeed;
		resetDone = false;
	}
	private void Take_Shot(int id)
	{
		if (id >= 0 && id < strikeInfo.Count)
		{
			strikeDir = strikeInfo[id].dir;
			strikeSpeed = strikeInfo[id].speed;
			striker.transform.position = strikeInfo[id].pos;
		}
		Take_Shot();
	}

	private void Reset_Tokens()
	{
		var pos = Vector3.zero;
		for (int t = 0; t < pucks.Length; t++)
		{
			pos.x = Random.Range(-spawnCube, spawnCube);
			pos.y = Random.Range(-spawnCube, spawnCube);
			pucks[t].velocity = Vector2.zero;
			pucks[t].transform.position = pos;
			pucks[t].gameObject.SetActive(true);
		}
	}
	#endregion

	#region UI_Events
	//Sadly, No Time for UI Manager YET
	public bool UI_DirectShot { set; get; } = true;
	public bool UI_BackShot { set; get; } = true;
	public bool UI_SideShot { set; get; } = true;
	public string UI_PlayerID { set; get; } = "1";
	
	public void UI_Calculate()
	{
		if (string.IsNullOrEmpty(UI_PlayerID))
			return;
		var plrID = int.Parse(UI_PlayerID) - 1; // 0 for First Index
		if (plrID < 0 || plrID > 3)
			return;
		Calc_BestShot(plrID);
	}
	public void UI_Fire()
	{
		UI_Calculate();
		Take_Shot(-1);
	}
	public void UI_ResetPucks()
	{
		striker.gameObject.SetActive(true);
		Reset_Tokens();
	}
	#endregion

#if UNITY_EDITOR
	[Header("Editor Dev Test")]
	[SerializeField] bool placeTokens = false;
	[SerializeField] bool calcDir = false;
	[SerializeField] bool visualize = false;
	[SerializeField] bool strike = false;
	[SerializeField] int shotID = -1;
	private void OnDrawGizmos()
	{
		if (placeTokens)
		{
			placeTokens = false;
			Reset_Tokens();
		}
		if (calcDir)
		{
			if (tPot && tPuck)
			{
				calcDir = false;
				Calc_BestShot(0);
			}
		}
		if (visualize)
		{
			visualize = false;
			Visualize_Strike(shotID);
		}
		if (strike)
		{
			strike = false;
			Take_Shot(shotID);
		}

		Gizmos.color = Color.blue;
		if (playLines != null)
			for (int p = 0; p < 4; p++)
				Gizmos.DrawLine(playLines[p].pos, playLines[p].pos + playLines[p].lenDir * playSideLen);
		if (playLines != null)
			for (int p = 4; p < 8; p++)
				Gizmos.DrawLine(playLines[p].pos, playLines[p].pos + playLines[p].lenDir * reflectSideLen);
		if (strikeInfo != null && strikeInfo.Count > 0)
		{
			for (int s = 0; s < strikeInfo.Count; s++)
			{
				if (strikeInfo[s].rVec != Vector2.zero)
				{
					Gizmos.color = Color.yellow;
					Gizmos.DrawLine(strikeInfo[s].tar, strikeInfo[s].tar + strikeInfo[s].rVec);
					Gizmos.DrawWireSphere(strikeInfo[s].tar + strikeInfo[s].rVec, strikerRadius);
				}
				else
				{
					Gizmos.color = Color.green;
				}
				Gizmos.DrawLine(strikeInfo[s].pos, strikeInfo[s].tar);
				Gizmos.DrawWireSphere(strikeInfo[s].tar, strikerRadius);
				Gizmos.DrawLine(pucks[strikeInfo[s].puckID].position, pots[strikeInfo[s].potID].position);
			}
		}
	}
#endif
}
