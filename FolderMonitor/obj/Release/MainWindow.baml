   M S B A M L   `   `   `  ����   FolderMonitor    FolderMonitor.MainWindow   \ XPresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    8x,http://schemas.microsoft.com/winfx/2006/xaml    \ XHardcodet.Wpf.TaskbarNotification, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null)tb http://www.hardcodet.net/taskbar  -   $��aMainWindow��  � Title$  Folder Monitor�� � SizeToContent$ WidthAndHeight=� � WindowStyle$ SingleBorderWindow=� � DataContext ��  �� RelativeSource �� *Self+.���� .+�o� .@�m� -   $
��Exit���� .@��� $��Folder List��.���� $��4,0,4,4q�.+��� Folders to monitor:�� -   $��FoldersList�� �� SelectionMode$ Single=�$
��Auto��$	��200������ *	Folders+ �� View �� .P��� $��Source�� �� Width$	 200�� �� CellTemplate �� .R��� $��CharacterEllipsis=����� *Source+�� $��Destination��$	 100�� �� .R��� $��CharacterEllipsis=����� *Dest+�� $ 24��$�� �� �� .R��� -   	 � Cursor$
	 Hand��$��Images/Delete.png��9� $��Add sources��$��0,5,0,0q�.���� .+��� 
 �� Orientation$
 
Horizontal=�.+��� -   $��	SourceBox��$	��5,3q� �� 
IsReadOnly . $	��260��$��22���� -   $��	SourceBtn��$��22��.��	Browse...�� $
 
Horizontal=�.+��� -   $��DestBox��$	��5,3q� . $	��260��$��22���� -   $��DestBtn��$��22��.��	Browse...�� -	   $��80��$��22��.��
Add source�� $��Options��.���� .+��� -
   $��CloseToTray�� d� 	IsChecked$
 TrueY�.��Close to tray�� -   $��RunOnStartup��$
 TrueY�.��Run on startup�� -   $��IntegrateXBMC��$
 TrueY�.��Integrate with XBMC� -   $��XBMCConnectionInfo��$��17,0,0,0q�$y�	Collapsed=� � RowDefinitions �� $
��Auto ��� $
��Auto ��� $
��Auto ��� $��22 � � ColumnDefinitions �� $��55 ��� $	��145 �.+��� $��0��$��0��$
��Left=�$��Center=�
Port Nr:�� -   $��XBMCPort��$��0��$��1��$��Right=��� $��1��$��0��$
��Left=�$��Center=�User:�� -   $��XBMCUser��$��1��$��1��$��Right=��� $��2��$��0��$
��Left=�$��Center=�	Password:�� -   $��XBMCPassword��$��2��$��1��$��Right=��� -   $��3��$��1��$��20��$��0,-2,0,0q�.��Save settings�� $
 
Horizontal=�.+��� &$What filetypes should NOT be copied:�� $��15��$��20��$��5,0,0,0q� � ToolTip$0 *Seperate file types with a ; - eg. dat;nfo��.��?�� $
 
Horizontal=�.+��� -   $��FilterDataTypes��$	��190���� -   .��Save�� $��Errors��.���� -   $��	ErrorList��$ Single=�$
��Auto��$	��200������ *Errors+ �� .P��� $��Error Description��$	 250�� �� .R��� $��CharacterEllipsis=����� *Description+ �� *Descriptioin+�� $ 45��$�� �� �� .R��� $
 
Horizontal=�.+��� -   $
	 Hand��$��14��$��0,0,2,0q�$��Images/Retry.png��y��� *
ShowReload+�� -   $
	 Hand��$��14��$��Images/Delete.png���� -   $��WindowStatusBar��.@��� .���� -   $��XBMCConnectionStatus��$
 
Horizontal=�$y�	Collapsed=�.+��� -   $��XBMCConnectionStatusImage��$��Images/xbmc.ico��$��14�� � ContextMenu �� .@�m� -   $��XBMCConnectionMenuHeader��$��Connect���� -   $��XBMCConnectionStatusText��$��5,0,0,0q�Not connected3  -Hardcodet.Wpf.TaskbarNotification.TaskbarIcon  -     Name$ 
NotifyIcon��   
IconSource$ /Images/tray.ico��   ToolTipText$ Folder Monitor�� �� .@�m� -   $
��Exit��