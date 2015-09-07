﻿//
// MainPage.xaml.cpp
// Implementation of the MainPage class.
//

#include "pch.h"
#include "MainPage.xaml.h"

using namespace ortc_standup;

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Controls::Primitives;
using namespace Windows::UI::Xaml::Data;
using namespace Windows::UI::Xaml::Input;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Navigation;

Windows::UI::Core::CoreDispatcher^ g_windowDispatcher;

MainPage::MainPage() : mediaEngine_(NULL)
{
	InitializeComponent();
}

ortc_standup::MainPage::~MainPage()
{
  if (mediaEngine_)
    delete mediaEngine_;
}

/// <summary>
/// Invoked when this page is about to be displayed in a Frame.
/// </summary>
/// <param name="e">Event data that describes how this page was reached.  The Parameter
/// property is typically used to configure the page.</param>
void MainPage::OnNavigatedTo(NavigationEventArgs^ e)
{
	(void) e;	// Unused parameter

	// TODO: Prepare page for display here.

	// TODO: If your application contains multiple pages, ensure that you are
	// handling the hardware Back button by registering for the
	// Windows::Phone::UI::Input::HardwareButtons.BackPressed event.
	// If you are using the NavigationHelper provided by some templates,
	// this event is handled for you.
}

void ortc_standup::MainPage::Page_Loaded(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e)
{
  g_windowDispatcher = Window::Current->Dispatcher;
  mediaEngine_ = MediaEngine::create(NULL).get();
  mediaEngine_->setStartStopButton(StartStopButton);
  mediaEngine_->setLocalMediaElement(LocalVideoMediaElement);
  mediaEngine_->setRemoteMediaElement(RemoteVideoMediaElement);
}

void ortc_standup::MainPage::StartStopButton_Click(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e)
{
  mediaEngine_->makeCall();
}
