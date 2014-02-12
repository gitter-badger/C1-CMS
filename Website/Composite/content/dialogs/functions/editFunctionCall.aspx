﻿<?xml version="1.0" encoding="UTF-8" ?>
<%@ Page Language="C#" AutoEventWireup="true" Inherits="CompositeEditFunctionCall.EditFunctionCall" EnableEventValidation="false" ValidateRequest="false" CodeFile="editFunctionCall.aspx.cs" %>
<%@ Register TagPrefix="aspui" Namespace="Composite.Core.WebClient.UiControlLib" Assembly="Composite" %>
<%@ Register TagPrefix="control1" Src="~/Composite/controls/FormsControls/FormUiControlTemplates/DeveloperTools/FunctionCallsDesigner.ascx" TagName="FunctionCallDesigner" %>
<html xmlns="http://www.w3.org/1999/xhtml" xmlns:ui="http://www.w3.org/1999/xhtml">
	<control:httpheaders ID="Httpheaders1" runat="server" />
    <head>
		<title>Composite.Management.Dialogs.Functions.EditFunctionCall</title>
		<control:styleloader ID="Styleloader1" runat="server" />
		<control:scriptloader ID="Scriptloader1" type="sub" runat="server" />
		<asp:PlaceHolder ID="HeaderPlaceHolder" runat="server" />
		<link rel="stylesheet" type="text/css" href="editFunctionCall.css.aspx"/> 
		<script type="text/javascript" src="EditFunctionCallDialogPageBinding.js"></script>
		<script type="text/javascript" src="EditFunctionCallTabBoxBinding.js"></script>
		
		<script type="text/javascript">
		    /*
		     * Hacks a bug where haywire would fire on dialog exit.
		     */
		    function exit() {
		        setTimeout(function () {
		            bindingMap.renderingdialogpage.onDialogAccept();
		            Application.unlock ( window, true ); // now what?
		        }, 0 );
		    }
		</script>
	</head>
	<body>
		<form id="Form1" runat="server">
		
			<!-- this textbox contains the markup when Finish is clicked -->
			<div style="display: none;">
				<textarea runat="server" ID="FunctionMarkup"></textarea>
				<asp:PlaceHolder ID="DialogDoAcceptPlaceHolder" runat="server" visible="false">
					<ui:binding onattach="exit()" />
				</asp:PlaceHolder>
			</div>
			
			<ui:dialogpage 
				binding="EditFunctionCallDialogPageBinding" 
				id="renderingdialogpage" 
				image="${icon:parameter_overloaded}" 
				width="879"
				height="410" 
				resizable="false"
				label="<%= this.DialogLabel %>">
				<asp:HiddenField runat="server" ID="hdnActiveTab" Value="<%# hdnActiveTab.Value %>"></asp:HiddenField>
				<ui:tabbox id="maintabbox" selectedindex="<%= BasicViewEnabled ? hdnActiveTab.Value : "0" %>" binding="EditFunctionCallTabBoxBinding">
					<ui:tabs>
					    <asp:PlaceHolder runat="server" ID="plhBasicTab">
					        <ui:tab label="Basic" id="basictab"></ui:tab>
					    </asp:PlaceHolder>
						<ui:tab label="Advanced" id="advancedtab"></ui:tab>
					</ui:tabs>
					<ui:tabpanels>
					    <asp:PlaceHolder runat="server" ID="plhBasicTabContent">
						    <ui:tabpanel id="BasicTabPanel">
						        <ui:scrollbox class="padded">
						            <asp:PlaceHolder ID="BasicPanel" runat="server">
							        </asp:PlaceHolder>
						        </ui:scrollbox>
                                <div id="errors" style="display: none" class="updatezone">
                                    <asp:PlaceHolder runat="server" ID="plhErrors"></asp:PlaceHolder>
                                </div>
						    </ui:tabpanel>
                        </asp:PlaceHolder>
						<ui:tabpanel id="AdvancedTabPanel">
							<asp:PlaceHolder ID="AdvancedPanel" runat="server">
								<control1:FunctionCallDesigner ID="FunctionCallDesigner" runat="server" />
							</asp:PlaceHolder>

						</ui:tabpanel>
					</ui:tabpanels>
				</ui:tabbox>
				<ui:dialogtoolbar>
					<ui:toolbarbody align="right" equalsize="true">
						<ui:toolbargroup>
							<ui:clickbutton label="${string:Website.Dialogs.LabelAccept}"
								oncommand="bindingMap.renderingdialogpage.onOk ()"  
								callbackid="buttonAccept" 
								focusable="true"/>
							<ui:clickbutton label="${string:Website.Dialogs.LabelCancel}" 
							 	response="cancel" 
							 	focusable="true"/> 
                        </ui:toolbargroup>
					</ui:toolbarbody>
				</ui:dialogtoolbar>
				
			</ui:dialogpage>
		</form>
	</body>
</html>
