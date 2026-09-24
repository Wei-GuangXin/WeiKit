/**
 *  控制台日志主题风格定义，存储不同风格的 HTML 头部代码。
 * **/
namespace WeiKit
{
	public static class ConsoleLogTheme
	{
		public enum ThemeType
		{
			/// <summary>
			/// 经典绿屏终端风（复古命令行）
			/// </summary>
			ClassicGreen,

			/// <summary>
			/// 现代暗黑风（平滑简洁）
			/// </summary>
			ModernDark,

			/// <summary>
			/// 明亮简洁风（适合打印或浅色环境）
			/// </summary>
			LightClean,

			SciFiConsole,

			/// <summary>
			/// 高对比警报风（强调错误与警告）
			/// </summary>
			HighContrastAlert,

			/// <summary>
			/// 黑客矩阵风（动态背景 + 动画效果）
			/// </summary>
			HackerMatrix,

			/// <summary>
			/// 霓虹赛博风（霓虹光效，未来科技感）
			/// </summary>
			NeonCyber,

			/// <summary>
			/// 暖色纸质感（类似纸张和墨水）
			/// </summary>
			WarmPaper,

			/// <summary>
			/// 深空宇宙风（星空背景，柔和渐变）
			/// </summary>
			DeepSpace,

			/// <summary>
			/// 简约专业蓝（现代企业风格）
			/// </summary>
			ProfessionalBlue,

			/// <summary>
			/// 多彩彩虹风（每个日志级别不同彩虹色）
			/// </summary>
			RainbowColor,

			/// <summary>
			/// 玻璃拟态风（毛玻璃透明效果）
			/// </summary>
			GlassMorphism,

			/// <summary>
			/// 复古CRT显示器（扫描线，弯曲边缘）
			/// </summary>
			VintageCRT
		}

		/// <summary>
		/// 获取指定主题类型的 HTML 头部代码（包含样式定义）
		/// </summary>
		/// <param name="theme">主题类型</param>
		/// <returns>HTML 字符串</returns>
		public static string GetHtmlHeader(ThemeType theme)
		{
			switch (theme)
			{
				case ThemeType.ClassicGreen:
					return GetClassicGreenTheme();
				case ThemeType.ModernDark:
					return GetModernDarkTheme();
				case ThemeType.LightClean:
					return GetLightCleanTheme();
				case ThemeType.SciFiConsole:
					return GetSciFiConsoleTheme();
				case ThemeType.HighContrastAlert:
					return GetHighContrastAlertTheme();
				case ThemeType.HackerMatrix:
					return GetHackerMatrixTheme();
				case ThemeType.NeonCyber:
					return GetNeonCyberTheme();
				case ThemeType.WarmPaper:
					return GetWarmPaperTheme();
				case ThemeType.DeepSpace:
					return GetDeepSpaceTheme();
				case ThemeType.ProfessionalBlue:
					return GetProfessionalBlueTheme();
				case ThemeType.RainbowColor:
					return GetRainbowColorTheme();
				case ThemeType.GlassMorphism:
					return GetGlassMorphismTheme();
				case ThemeType.VintageCRT:
					return GetVintageCRTTheme();
				default:
					return GetModernDarkTheme();
			}
		}

		private static string GetClassicGreenTheme()
		{
			return @"
<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background-color: #000000;
            color: #00ff00;
            font-family: 'Courier New', monospace;
            margin: 0;
            padding: 10px;
            background-image: linear-gradient(rgba(0, 50, 0, 0.2) 1px, transparent 1px),
                            linear-gradient(90deg, rgba(0, 50, 0, 0.2) 1px, transparent 1px);
            background-size: 20px 20px;
        }
        .t { color: #00ff00; }
        .i { color: #33ff33; }
        .w { color: #ffff00; }
        .e { color: #ff0000; font-weight: bold; }
        pre { margin: 0; }
    </style>
</head>
<body>";
		}

		private static string GetModernDarkTheme()
		{
			return @"
<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background-color: #1e1e1e;
            color: #d4d4d4;
            font-family: Consolas, 'Courier New', monospace;
            margin: 0;
            padding: 10px;
            border-left: 4px solid #007acc;
        }
        .t { color: #cccccc; }
        .i { color: #66cc66; }
        .w { color: #d7ba7d; }
        .e { color: #f44747; font-weight: bold; }
        pre { margin: 5px 0; }
    </style>
</head>
<body>";
		}

		private static string GetLightCleanTheme()
		{
			return @"
<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background-color: #ffffff;
            color: #333333;
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 10px;
            font-size: 14px;
        }
        .t { color: #333333; }
        .i { color: #008800; }
        .w { color: #bb8800; }
        .e { color: #dd0000; font-weight: bold; background-color: #ffeaea; padding: 1px 3px; border-radius: 3px; }
        pre { margin: 3px 0; }
    </style>
</head>
<body>";
		}

		private static string GetSciFiConsoleTheme()
		{
			return @"<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background-color: #0a0f0f;
            background-image: 
                radial-gradient(circle at 20% 40%, rgba(0, 255, 255, 0.08) 0%, transparent 30%),
                linear-gradient(135deg, #0a0c0b 0%, #0e1a1a 100%);
            color: #b0f0f0;
            font-family: 'Courier New', Courier, monospace;
            margin: 0;
            padding: 25px;
            font-size: 14px;
            line-height: 1.5;
            min-height: 100vh;
            position: relative;
            overflow-x: auto;
            text-shadow: 0 0 3px rgba(0, 255, 255, 0.3);
        }

        /* 日志级别样式 —— 科幻霓虹效果 */
        .t {
            color: #b0f0f0;
            text-shadow: 0 0 5px rgba(0, 255, 255, 0.5);
            transition: all 0.25s ease;
        }
        .i {
            color: #0ff;
            text-shadow: 0 0 8px #0ff, 0 0 15px #0ff;
        }
        .w {
            color: #ffb347;
            text-shadow: 0 0 8px #ffb347, 0 0 15px #ff8c00;
        }
        .e {
            color: #ff3b6f;
            font-weight: bold;
            text-shadow: 0 0 8px #ff3b6f, 0 0 20px #ff1a4f, 0 0 30px #ff0033;
            background-color: rgba(255, 59, 111, 0.1);
            padding: 2px 6px;
            border-radius: 4px;
            animation: errorPulse 1.8s infinite ease-in-out;
        }

        /* 错误脉冲动画 */
        @keyframes errorPulse {
            0% { text-shadow: 0 0 5px #ff3b6f, 0 0 15px #ff1a4f; }
            50% { text-shadow: 0 0 20px #ff3b6f, 0 0 35px #ff1a4f, 0 0 50px #ff0033; }
            100% { text-shadow: 0 0 5px #ff3b6f, 0 0 15px #ff1a4f; }
        }

        /* 鼠标悬停互动 —— 光轨效果 */
        .t:hover, .i:hover, .w:hover, .e:hover {
            background-color: rgba(0, 255, 255, 0.08);
            border-left: 3px solid currentColor;
            padding-left: 10px;
            transform: translateX(3px);
            transition: all 0.2s ease;
            cursor: pointer;
        }

        /* 科幻背景 SVG 容器 —— 置于底层，不影响文本操作 */
        .scifi-bg {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            pointer-events: none;
            z-index: -1;
            overflow: hidden;
        }

        /* 可选的扫描线效果（纯CSS点缀） */
        body::after {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: repeating-linear-gradient(0deg, rgba(0, 255, 255, 0.02) 0px, rgba(0, 0, 0, 0.2) 2px, transparent 3px);
            pointer-events: none;
            z-index: -1;
        }

        /* 确保所有文本在背景之上 */
        pre, div, p {
            position: relative;
            z-index: 1;
        }
    </style>
</head>
<body>
    <!-- 简约动态 SVG 背景 —— 网格、脉冲点、流动线条 -->
    <div class=""scifi-bg"">
        <svg width=""100%"" height=""100%"" xmlns=""http://www.w3.org/2000/svg"" style=""display: block;"">
            <defs>
                <!-- 半透明网格图案 -->
                <pattern id=""grid"" x=""0"" y=""0"" width=""40"" height=""40"" patternUnits=""userSpaceOnUse"">
                    <path d=""M 40 0 L 0 0 0 40"" fill=""none"" stroke=""rgba(0, 255, 255, 0.12)"" stroke-width=""0.7""/>
                </pattern>
            </defs>
            <rect width=""100%"" height=""100%"" fill=""url(#grid)"" />

            <!-- 随机脉冲光点 (SMIL动画) -->
            <circle cx=""15%"" cy=""25%"" r=""3"" fill=""rgba(0, 255, 255, 0.25)"">
                <animate attributeName=""r"" values=""3;7;3"" dur=""4s"" repeatCount=""indefinite"" />
                <animate attributeName=""opacity"" values=""0.3;0.7;0.3"" dur=""4s"" repeatCount=""indefinite"" />
            </circle>
            <circle cx=""85%"" cy=""70%"" r=""4"" fill=""rgba(255, 75, 120, 0.2)"">
                <animate attributeName=""r"" values=""4;9;4"" dur=""5s"" repeatCount=""indefinite"" />
                <animate attributeName=""opacity"" values=""0.2;0.6;0.2"" dur=""5s"" repeatCount=""indefinite"" />
            </circle>
            <circle cx=""45%"" cy=""85%"" r=""2"" fill=""rgba(255, 200, 50, 0.2)"">
                <animate attributeName=""r"" values=""2;5;2"" dur=""3.5s"" repeatCount=""indefinite"" />
            </circle>

            <!-- 动态流动的数据线 -->
            <path d=""M -10% 30% Q 20% 20%, 40% 35% T 110% 30%"" stroke=""rgba(0, 200, 255, 0.25)"" stroke-width=""1.5"" fill=""none"">
                <animate attributeName=""d"" 
                         values=""M -10% 30% Q 20% 20%, 40% 35% T 110% 30%;
                                 M -10% 45% Q 20% 35%, 40% 50% T 110% 45%;
                                 M -10% 30% Q 20% 20%, 40% 35% T 110% 30%""
                         dur=""10s"" 
                         repeatCount=""indefinite"" />
            </path>
            <path d=""M -10% 70% Q 30% 55%, 60% 75% T 110% 70%"" stroke=""rgba(255, 100, 150, 0.2)"" stroke-width=""1.2"" fill=""none"">
                <animate attributeName=""d"" 
                         values=""M -10% 70% Q 30% 55%, 60% 75% T 110% 70%;
                                 M -10% 82% Q 30% 67%, 60% 87% T 110% 82%;
                                 M -10% 70% Q 30% 55%, 60% 75% T 110% 70%""
                         dur=""12s"" 
                         repeatCount=""indefinite"" />
            </path>
        </svg>
    </div>";
		}

		private static string GetHighContrastAlertTheme()
		{
			return @"
<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background-color: #000000;
            color: white;
            font-family: 'Courier New', monospace;
            margin: 0;
            padding: 10px;
        }
        .t { color: white; }
        .i { color: lime; }
        .w { color: orange; text-shadow: 0 0 5px yellow; }
        .e { 
            color: red; 
            font-weight: bold; 
            text-shadow: 0 0 10px red; 
            font-size: 1.1em; 
            animation: pulse 1.5s infinite alternate; 
        }
        @keyframes pulse {
            from { opacity: 0.7; }
            to { opacity: 1; }
        }
        pre { margin: 2px 0; }
    </style>
</head>
<body>";
		}

		private static string GetHackerMatrixTheme()
		{
			return @"
<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background-color: #000000;
            color: #00ff00;
            font-family: 'Courier New', monospace;
            margin: 0;
            padding: 10px;
            background-image: radial-gradient(circle at center, #003300 0%, #000000 70%);
            overflow-y: scroll;
        }
        ::selection { background: #005500; }
        .t { color: #00ff00; opacity: 0.8; }
        .i { color: #66ff66; }
        .w { color: #ffbb33; animation: flicker 2s infinite; }
        .e { color: #ff3333; font-weight: bold; text-shadow: 0 0 3px #ff0000; animation: shake 0.5s infinite; }
        
        @keyframes flicker {
            0%, 19%, 21%, 23%, 25%, 54%, 56%, 100% { opacity: 1; }
            20%, 22%, 24%, 55% { opacity: 0.5; }
        }
        @keyframes shake {
            0%, 100% { transform: translateX(0); }
            25% { transform: translateX(-1px); }
            75% { transform: translateX(1px); }
        }
        pre { margin: 1px 0; }
    </style>
</head>
<body>";
		}

		private static string GetNeonCyberTheme()
		{
			return @"
<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background-color: #0a0a14;
            color: #00f3ff;
            font-family: 'Courier New', monospace;
            margin: 0;
            padding: 10px;
            background-image: 
                radial-gradient(circle at 20% 30%, #1a1a2e 0%, transparent 50%),
                radial-gradient(circle at 80% 70%, #16213e 0%, transparent 50%);
        }
        .t { color: #00f3ff; text-shadow: 0 0 5px #00f3ff; }
        .i { color: #9d00ff; text-shadow: 0 0 5px #9d00ff; }
        .w { color: #ffcc00; text-shadow: 0 0 5px #ffcc00; }
        .e { 
            color: #ff0055; 
            text-shadow: 0 0 10px #ff0055, 0 0 20px #ff0055;
            animation: neonFlicker 2s infinite alternate;
        }
        @keyframes neonFlicker {
            0%, 19%, 21%, 23%, 25%, 54%, 56%, 100% { opacity: 1; }
            20%, 24%, 55% { opacity: 0.7; }
        }
        pre {
            margin: 2px 0;
            border-left: 2px solid #00f3ff;
            padding-left: 8px;
        }
    </style>
</head>
<body>";
		}

		private static string GetWarmPaperTheme()
		{
			return @"
<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background-color: #f5e9d5;
            color: #3a2615;
            font-family: Georgia, 'Times New Roman', serif;
            margin: 10px;
            padding: 20px;
            border: 1px solid #d4c4a8;
            box-shadow: 2px 2px 8px rgba(0,0,0,0.1);
            background-image: linear-gradient(to bottom, #f5e9d5 0%, #f0e1c8 100%);
        }
        .t { color: #3a2615; }
        .i { color: #2d5016; }
        .w { color: #8a6d0b; }
        .e { 
            color: #a52a2a; 
            font-weight: bold;
            border-bottom: 1px solid #a52a2a;
        }
        pre {
            margin: 4px 0;
            padding: 4px;
            background-color: rgba(255, 255, 255, 0.5);
            border-radius: 2px;
        }
    </style>
</head>
<body>";
		}

		private static string GetDeepSpaceTheme()
		{
			return @"
<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background-color: #0b0b2d;
            color: #a3c7ff;
            font-family: 'Segoe UI', Tahoma, sans-serif;
            margin: 0;
            padding: 10px;
            background-image: 
                radial-gradient(circle at 10% 20%, #1a1a4a 0%, transparent 30%),
                radial-gradient(circle at 90% 80%, #2d1b69 0%, transparent 30%),
                url('data:image/svg+xml,<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""100""><circle cx=""10"" cy=""10"" r=""0.5"" fill=""white"" opacity=""0.6""/><circle cx=""30"" cy=""70"" r=""0.8"" fill=""white"" opacity=""0.8""/><circle cx=""80"" cy=""40"" r=""0.3"" fill=""white"" opacity=""0.5""/></svg>');
        }
        .t { color: #a3c7ff; }
        .i { color: #4dccff; }
        .w { color: #ffcc66; text-shadow: 0 0 5px rgba(255, 204, 102, 0.5); }
        .e { 
            color: #ff6680; 
            font-weight: bold;
            text-shadow: 0 0 8px #ff6680;
            animation: spacePulse 2s infinite;
        }
        @keyframes spacePulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.7; }
        }
        pre {
            margin: 3px 0;
            padding-left: 5px;
            border-left: 1px solid rgba(163, 199, 255, 0.3);
        }
    </style>
</head>
<body>";
		}

		private static string GetProfessionalBlueTheme()
		{
			return @"
<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background-color: #f8fafc;
            color: #2d3748;
            font-family: 'SF Mono', Monaco, 'Cascadia Code', Consolas, monospace;
            margin: 0;
            padding: 15px;
            border-top: 4px solid #2b6cb0;
        }
        .t { color: #2d3748; }
        .i { color: #2f855a; }
        .w { color: #b7791f; }
        .e { 
            color: #c53030; 
            font-weight: bold;
            background: linear-gradient(90deg, rgba(197,48,48,0.1) 0%, transparent 100%);
            padding: 2px 6px;
            border-radius: 3px;
        }
        pre {
            margin: 4px 0;
            padding: 6px 8px;
            background-color: #edf2f7;
            border-radius: 4px;
            border: 1px solid #e2e8f0;
        }
    </style>
</head>
<body>";
		}

		private static string GetRainbowColorTheme()
		{
			return @"
<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background-color: #f0f0f0;
            color: #333;
            font-family: 'Comic Sans MS', 'Segoe UI', sans-serif;
            margin: 0;
            padding: 10px;
            background-image: linear-gradient(45deg, #f0f0f0 25%, #ffffff 25%, #ffffff 50%, #f0f0f0 50%, #f0f0f0 75%, #ffffff 75%, #ffffff 100%);
            background-size: 20px 20px;
        }
        .t { color: #666; }
        .i { color: #ff6b6b; }
        .w { color: #ffa726; }
        .e { 
            color: #5c6bc0; 
            font-weight: bold;
            background: linear-gradient(90deg, #ff6b6b, #ffa726, #ffca28, #a5d6a7, #5c6bc0);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            animation: rainbowSlide 3s infinite linear;
        }
        @keyframes rainbowSlide {
            0% { background-position: 0% 50%; }
            100% { background-position: 200% 50%; }
        }
        pre {
            margin: 2px 0;
            padding: 4px;
            border-radius: 4px;
            background-color: white;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
        }
    </style>
</head>
<body>";
		}

		private static string GetGlassMorphismTheme()
		{
			return @"
<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #333;
            font-family: 'Segoe UI', system-ui, sans-serif;
            margin: 20px;
            padding: 20px;
            min-height: calc(100vh - 40px);
        }
        .glass-container {
            background: rgba(255, 255, 255, 0.2);
            backdrop-filter: blur(10px);
            border-radius: 12px;
            padding: 20px;
            border: 1px solid rgba(255, 255, 255, 0.3);
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
        }
        .t { color: rgba(255, 255, 255, 0.9); }
        .i { color: #a5ffc9; }
        .w { color: #ffd166; }
        .e { 
            color: #ff6b6b;
            font-weight: bold;
            background: rgba(255, 107, 107, 0.2);
            padding: 2px 8px;
            border-radius: 6px;
            backdrop-filter: blur(4px);
        }
        pre {
            margin: 6px 0;
            padding: 8px 12px;
            background: rgba(255, 255, 255, 0.1);
            border-radius: 8px;
            border: 1px solid rgba(255, 255, 255, 0.2);
        }
    </style>
</head>
<body>
<div class=""glass-container"">";
		}

		private static string GetVintageCRTTheme()
		{
			return @"
<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <style>
        body {
            background-color: #1a1a1a;
            color: #c0c0c0;
            font-family: 'VT323', 'Courier New', monospace;
            margin: 20px;
            padding: 20px;
            position: relative;
            overflow: hidden;
            font-size: 18px;
            border: 15px solid #333;
            border-radius: 8px;
            box-shadow: inset 0 0 30px rgba(0, 0, 0, 0.8);
        }
        body::before {
            content: '';
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: linear-gradient(rgba(0, 150, 0, 0.1) 1px, transparent 1px);
            background-size: 100% 2px;
            pointer-events: none;
            animation: scanlines 10s linear infinite;
        }
        body::after {
            content: '';
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: radial-gradient(ellipse at center, transparent 50%, rgba(0,0,0,0.5) 100%);
            pointer-events: none;
        }
        .t { color: #c0c0c0; }
        .i { color: #90ee90; }
        .w { color: #ffcc00; }
        .e { 
            color: #ff4444;
            font-weight: bold;
            text-shadow: 0 0 5px #ff4444;
            animation: crtFlicker 0.5s infinite;
        }
        @keyframes scanlines {
            0% { transform: translateY(0); }
            100% { transform: translateY(-100%); }
        }
        @keyframes crtFlicker {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.8; }
        }
        pre {
            margin: 4px 0;
            padding: 4px;
            background-color: rgba(0, 0, 0, 0.3);
            border: 1px solid rgba(100, 100, 100, 0.3);
        }
    </style>
</head>
<body>";
		}
	}
}